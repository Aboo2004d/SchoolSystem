using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers;

[ApiController]
[Route("api/transfers")]
[AuthorizeRoles(RoleNames.Admin, RoleNames.DirectorateManager)]
public sealed class TransferRequestsApiController : ControllerBase
{
    private readonly SystemSchoolDbContext _db;
    private readonly ISessionValidatorService _sessions;
    private readonly UserManager<ApplicationUser> _users;

    public TransferRequestsApiController(SystemSchoolDbContext db, ISessionValidatorService sessions,
        UserManager<ApplicationUser> users)
    {
        _db = db;
        _sessions = sessions;
        _users = users;
    }

    [HttpGet("options")]
    public async Task<IActionResult> Options(CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("Transfers/Options");
        if (!scope.Allowed) return Forbid();
        var schools = _db.Schools.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted);
        if (scope.DirectorateId.HasValue) schools = schools.Where(x => x.DirectorateId == scope.DirectorateId);
        return Ok(await schools.OrderBy(x => x.Name).Select(x => new
        {
            x.Id, x.Name, directorate = x.Directorate.Name, ministry = x.Directorate.Ministry.Name,
            classes = x.TheClasses.Where(c => !c.IsDeleted && !c.IsDeletedSchool)
                .OrderBy(c => c.NumberClass).ThenBy(c => c.Section).Select(c => new { c.Id, c.Name }).ToList()
        }).ToListAsync(cancellationToken));
    }
    [HttpGet]
    public async Task<IActionResult> List(string? direction, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("Transfers/List");
        if (!scope.Allowed) return Forbid();
        var query = _db.TransferRequests.AsNoTracking();
        if (scope.DirectorateId.HasValue)
            query = string.Equals(direction, "outgoing", StringComparison.OrdinalIgnoreCase)
                ? query.Where(x => x.DestinationDirectorateId == scope.DirectorateId)
                : query.Where(x => x.SourceDirectorateId == scope.DirectorateId);

        return Ok(await query.OrderByDescending(x => x.CreatedAtUtc).Select(x => new
        {
            x.Id, x.SubjectType, identityNumber = x.SubjectIdentityNumber, x.Status,
            x.SourceMinistryId, x.DestinationMinistryId, x.SourceDirectorateId,
            x.DestinationDirectorateId, x.SourceSchoolId, x.DestinationSchoolId,
            x.SourceClassId, x.DestinationClassId, x.Reason, x.DecisionNote, x.CreatedAtUtc,
            x.DecidedAtUtc, x.CompletedAtUtc
        }).ToListAsync(cancellationToken));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateTransferRequest request, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("Transfers/Create");
        if (!scope.Allowed) return Forbid();
        var type = NormalizeType(request.SubjectType);
        if (type is null) return BadRequest(new { message = "نوع الشخص المطلوب نقله غير صالح." });

        var destination = await _db.Schools.AsNoTracking().Where(x => x.Id == request.DestinationSchoolId && x.IsActive && !x.IsDeleted)
            .Select(x => new { x.Id, x.DirectorateId, MinistryId = x.Directorate.MinistryId }).SingleOrDefaultAsync(cancellationToken);
        if (destination is null) return BadRequest(new { message = "المدرسة المستقبلة غير متاحة." });
        if (scope.DirectorateId.HasValue && destination.DirectorateId != scope.DirectorateId)
            return Forbid();

        var subject = await ResolveSubjectAsync(type, request.IdentityNumber, cancellationToken);
        if (subject is null) return NotFound(new { message = "لم يتم العثور على الشخص برقم الهوية المدخل." });
        if (subject.SourceSchoolId == destination.Id && type != TransferSubjectTypes.Student)
            return Conflict(new { message = "الشخص مرتبط بالمدرسة المستقبلة بالفعل." });
        if (type == TransferSubjectTypes.Student && !request.DestinationClassId.HasValue)
            return BadRequest(new { message = "يجب تحديد الصف المستقبل للطالب." });
        if (request.DestinationClassId.HasValue && !await _db.TheClasses.AnyAsync(x => x.Id == request.DestinationClassId && x.IdSchool == destination.Id && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken))
            return BadRequest(new { message = "الصف المحدد لا يتبع المدرسة المستقبلة." });
        if (await _db.TransferRequests.AnyAsync(x => x.SubjectType == type && x.SubjectIdentityNumber == request.IdentityNumber &&
            (x.Status == TransferStatuses.PendingSourceApproval || x.Status == TransferStatuses.PendingDestinationApproval), cancellationToken))
            return Conflict(new { message = "يوجد طلب نقل معلق للشخص نفسه." });

        var user = await _users.GetUserAsync(User);
        if (user is null) return Forbid();
        var transfer = new TransferRequest
        {
            Id = Guid.NewGuid(), SubjectType = type, SubjectId = subject.Id,
            SubjectIdentityNumber = request.IdentityNumber, SourceSchoolId = subject.SourceSchoolId,
            DestinationSchoolId = destination.Id, SourceDirectorateId = subject.SourceDirectorateId,
            DestinationDirectorateId = destination.DirectorateId, SourceMinistryId = subject.SourceMinistryId,
            DestinationMinistryId = destination.MinistryId, SourceClassId = subject.SourceClassId,
            DestinationClassId = request.DestinationClassId, RequestedByUserId = user.Id,
            Reason = request.Reason?.Trim(), Status = TransferStatuses.PendingSourceApproval
        };

        _db.TransferRequests.Add(transfer);
        if (subject.SourceDirectorateId == destination.DirectorateId)
            await CompleteAsync(transfer, user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(List), new { id = transfer.Id }, new { transfer.Id, transfer.Status,
            message = transfer.Status == TransferStatuses.Completed ? "تم النقل مباشرة داخل المديرية." : "تم إرسال طلب النقل إلى الجهة المالكة." });
    }

    [HttpPatch("{id:guid}/decision")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Decide(Guid id, DecideTransferRequest request, CancellationToken cancellationToken)
    {
        var scope = await ScopeAsync("Transfers/Decide");
        if (!scope.Allowed) return Forbid();
        var transfer = await _db.TransferRequests.SingleOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (transfer is null) return NotFound(new { message = "طلب النقل غير موجود." });
        if (transfer.Status != TransferStatuses.PendingSourceApproval && transfer.Status != TransferStatuses.PendingDestinationApproval)
            return Conflict(new { message = "تمت معالجة طلب النقل مسبقًا." });
        if (scope.DirectorateId.HasValue && transfer.SourceDirectorateId != scope.DirectorateId)
            return Forbid();
        var user = await _users.GetUserAsync(User);
        if (user is null) return Forbid();

        transfer.DecidedByUserId = user.Id;
        transfer.DecidedAtUtc = DateTime.UtcNow;
        transfer.DecisionNote = request.Note?.Trim();
        if (!request.Approve)
            transfer.Status = TransferStatuses.Rejected;
        else
            await CompleteAsync(transfer, user.Id, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { transfer.Id, transfer.Status, message = request.Approve ? "تمت الموافقة وتنفيذ النقل." : "تم رفض طلب النقل." });
    }

    private async Task CompleteAsync(TransferRequest transfer, Guid decidedBy, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (transfer.SubjectType == TransferSubjectTypes.Student)
        {
            if (!transfer.DestinationClassId.HasValue) throw new InvalidOperationException("Destination class is required.");
            var active = await _db.StudentEnrollments.Where(x => x.StudentId == transfer.SubjectId && x.IsActive).ToListAsync(cancellationToken);
            foreach (var item in active) { item.IsActive = false; item.EndedAtUtc = now; }
            _db.StudentEnrollments.Add(new StudentEnrollment { Id = Guid.NewGuid(), StudentId = transfer.SubjectId,
                SchoolId = transfer.DestinationSchoolId!.Value, ClassId = transfer.DestinationClassId.Value, IsActive = true, StartedAtUtc = now });
            var student = await _db.Students.SingleAsync(x => x.Id == transfer.SubjectId, cancellationToken);
            student.IdSchool = transfer.DestinationSchoolId; student.IdClass = transfer.DestinationClassId;
            student.IsDeletedStudent = student.IsDeletedSchool = student.IsDeletedClass = false;
            if (student.ApplicationUserId.HasValue)
            {
                var account = await _db.Users.FindAsync(new object[] { student.ApplicationUserId.Value }, cancellationToken);
                if (account is not null) account.IsActive = true;
            }
        }
        else if (transfer.SubjectType == TransferSubjectTypes.Teacher)
        {
            var active = await _db.TeacherPlacements.Where(x => x.TeacherId == transfer.SubjectId && x.IsActive && x.IsPrimary).ToListAsync(cancellationToken);
            foreach (var item in active) { item.IsActive = false; item.EndedAtUtc = now; item.IsPrimary = false; }
            _db.TeacherPlacements.Add(new TeacherPlacement { Id = Guid.NewGuid(), TeacherId = transfer.SubjectId,
                SchoolId = transfer.DestinationSchoolId!.Value, IsPrimary = true, IsActive = true, StartedAtUtc = now });
            var teacher = await _db.Teachers.SingleAsync(x => x.Id == transfer.SubjectId, cancellationToken);
            teacher.IdSchool = transfer.DestinationSchoolId; teacher.IsDeleted = teacher.IsDeletedSchool = false;
            if (teacher.ApplicationUserId.HasValue)
            {
                var account = await _db.Users.FindAsync(new object[] { teacher.ApplicationUserId.Value }, cancellationToken);
                if (account is not null) account.IsActive = true;
            }
        }
        else
        {
            var active = await _db.SchoolManagerAssignments.Where(x => x.ManagerId == transfer.SubjectId && x.IsActive && x.IsPrimary).ToListAsync(cancellationToken);
            foreach (var item in active) { item.IsActive = false; item.EndedAtUtc = now; item.IsPrimary = false; }
            _db.SchoolManagerAssignments.Add(new SchoolManagerAssignment { Id = Guid.NewGuid(), ManagerId = transfer.SubjectId,
                SchoolId = transfer.DestinationSchoolId!.Value, IsPrimary = true, IsActive = true, StartedAtUtc = now });
            var manager = await _db.Menegars.SingleAsync(x => x.Id == transfer.SubjectId, cancellationToken);
            manager.IdSchool = transfer.DestinationSchoolId; manager.IsDeleted = manager.IsDeletedSchool = false;
            if (manager.ApplicationUserId.HasValue)
            {
                var account = await _db.Users.FindAsync(new object[] { manager.ApplicationUserId.Value }, cancellationToken);
                if (account is not null) account.IsActive = true;
            }
        }
        transfer.DecidedByUserId = decidedBy; transfer.DecidedAtUtc = now;
        transfer.CompletedAtUtc = now; transfer.Status = TransferStatuses.Completed;
    }

    private async Task<SubjectLocation?> ResolveSubjectAsync(string type, int identityNumber, CancellationToken cancellationToken)
    {
        if (type == TransferSubjectTypes.Teacher)
            return await _db.Teachers.AsNoTracking().Where(x => x.IdNumber == identityNumber && x.IdSchool.HasValue)
                .Select(x => new SubjectLocation(x.Id, x.IdSchool!.Value, x.IdSchoolNavigation!.DirectorateId,
                    x.IdSchoolNavigation.Directorate.MinistryId, null)).SingleOrDefaultAsync(cancellationToken);
        if (type == TransferSubjectTypes.Student)
            return await _db.Students.AsNoTracking().Where(x => x.IdNumber == identityNumber && x.IdSchool.HasValue)
                .Select(x => new SubjectLocation(x.Id, x.IdSchool!.Value, x.IdSchoolNavigation!.DirectorateId,
                    x.IdSchoolNavigation.Directorate.MinistryId, x.IdClass)).SingleOrDefaultAsync(cancellationToken);
        return await _db.Menegars.AsNoTracking().Where(x => x.IdNumber == identityNumber && x.IdSchool.HasValue)
            .Select(x => new SubjectLocation(x.Id, x.IdSchool!.Value, x.IdSchoolNavigation!.DirectorateId,
                x.IdSchoolNavigation.Directorate.MinistryId, null)).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<(bool Allowed, Guid? DirectorateId)> ScopeAsync(string source)
    {
        if (User.IsInRole(RoleNames.Admin)) return (true, null);
        var access = await _sessions.ValidateDirectorateManagerSessionAsync(HttpContext, source);
        return (access.IsValid, access.IsValid ? access.DirectorateId : null);
    }

    private static string? NormalizeType(string? type) => type?.Trim().ToLowerInvariant() switch
    {
        "teacher" or "معلم" => TransferSubjectTypes.Teacher,
        "student" or "طالب" => TransferSubjectTypes.Student,
        "manager" or "schoolmanager" or "مدير" => TransferSubjectTypes.SchoolManager,
        _ => null
    };

    private sealed record SubjectLocation(Guid Id, Guid SourceSchoolId, Guid SourceDirectorateId,
        Guid SourceMinistryId, Guid? SourceClassId);
}
