using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers;

[ApiController]
[Route("api/ministry")]
[AuthorizeMinistry]
public sealed class MinistryApiController : ControllerBase
{
    private readonly SystemSchoolDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    public MinistryApiController(SystemSchoolDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (User.IsInRole(RoleNames.MinistryManager) && !ministryId.HasValue) return Forbid();
        var ministryQuery = _db.Ministries.Where(x => !ministryId.HasValue || x.Id == ministryId);
        var directorateQuery = _db.Directorates.Where(x => !ministryId.HasValue || x.MinistryId == ministryId);
        var schools = _db.Schools.Where(x => !x.IsDeleted && (!ministryId.HasValue || x.Directorate.MinistryId == ministryId));
        var schoolIds = schools.Select(x => x.Id);
        return Ok(new
        {
            ministries = await ministryQuery.CountAsync(cancellationToken),
            activeMinistries = await ministryQuery.CountAsync(x => x.IsActive, cancellationToken),
            directorates = await directorateQuery.CountAsync(cancellationToken),
            activeDirectorates = await directorateQuery.CountAsync(x => x.IsActive, cancellationToken),
            schools = await schools.CountAsync(cancellationToken),
            activeSchools = await schools.CountAsync(x => x.IsActive, cancellationToken),
            directorateManagers = await _db.DirectorateManagers.CountAsync(x => !x.IsDeleted && (!ministryId.HasValue || x.Directorate.MinistryId == ministryId), cancellationToken),
            schoolManagers = await _db.Menegars.CountAsync(x => !x.IsDeleted && x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value), cancellationToken),
            teachers = await _db.Teachers.CountAsync(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken),
            students = await _db.Students.CountAsync(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeletedStudent && !x.IsDeletedSchool, cancellationToken)
        });
    }

    [HttpGet("ministries")]
    public async Task<IActionResult> Ministries(CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (User.IsInRole(RoleNames.MinistryManager) && !ministryId.HasValue) return Forbid();
        return Ok(await _db.Ministries.AsNoTracking().Where(x => !ministryId.HasValue || x.Id == ministryId)
        .OrderBy(x => x.Name).Select(x => new
        {
            x.Id, x.Code, x.Name, x.IsActive,
            directorates = x.Directorates.Count,
            schools = x.Directorates.SelectMany(d => d.Schools).Count(s => !s.IsDeleted),
            teachers = x.Directorates.SelectMany(d => d.Schools).SelectMany(s => s.TeacherPlacements)
                .Where(p => p.IsActive).Select(p => p.TeacherId).Distinct().Count(),
            students = x.Directorates.SelectMany(d => d.Schools).SelectMany(s => s.StudentEnrollments)
                .Count(e => e.IsActive)
        }).ToListAsync(cancellationToken));
    }
    [HttpGet("directorates")]
    public async Task<IActionResult> Directorates(CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (User.IsInRole(RoleNames.MinistryManager) && !ministryId.HasValue) return Forbid();
        return Ok(await _db.Directorates.AsNoTracking().Where(x => !ministryId.HasValue || x.MinistryId == ministryId)
        .OrderBy(x => x.Name).Select(x => new
        {
            x.Id, x.Code, x.Name, x.City, x.Area, x.Phone, x.Email, x.IsActive, ministry = x.Ministry.Name, ministryCode = x.Ministry.Code,
            manager = x.Manager != null && !x.Manager.IsDeleted ? x.Manager.Name : null,
            managerAccountActive = x.Manager != null && !x.Manager.IsDeleted && x.Manager.ApplicationUser != null && x.Manager.ApplicationUser.IsActive,
            schools = x.Schools.Count(s => !s.IsDeleted),
            activeSchools = x.Schools.Count(s => !s.IsDeleted && s.IsActive),
            teachers = x.Schools.Where(s => !s.IsDeleted).SelectMany(s => s.Teachers).Count(t => !t.IsDeleted && !t.IsDeletedSchool),
            students = x.Schools.Where(s => !s.IsDeleted).SelectMany(s => s.Students).Count(s => !s.IsDeletedStudent && !s.IsDeletedSchool)
        }).ToListAsync(cancellationToken));

    }

    [HttpGet("create-options")]
    public async Task<IActionResult> CreateOptions(string type, Guid? organizationId, CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (!ministryId.HasValue && !User.IsInRole(RoleNames.Admin)) return Forbid();
        var directorates = _db.Directorates.AsNoTracking().Where(x => x.IsActive && (!ministryId.HasValue || x.MinistryId == ministryId));
        if (type == "directorateManager") directorates = directorates.Where(x => x.Manager == null || x.Manager.IsDeleted);
        var schools = _db.Schools.AsNoTracking().Where(x => x.IsActive && !x.IsDeleted && (!ministryId.HasValue || x.Directorate.MinistryId == ministryId));
        if (type == "schoolManager") schools = schools.Where(x => !x.Menegars.Any(m => !m.IsDeleted && !m.IsDeletedSchool));
        var classes = organizationId.HasValue ? await _db.TheClasses.AsNoTracking().Where(x => x.IdSchool == organizationId && !x.IsDeleted && !x.IsDeletedSchool).OrderBy(x => x.NumberClass).ThenBy(x => x.Section).Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken) : [];
        return Ok(new { directorates = await directorates.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.Code }).ToListAsync(cancellationToken), schools = await schools.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, directorate = x.Directorate.Name }).ToListAsync(cancellationToken), classes, statuses = await _db.StatusSchools.AsNoTracking().Select(x => new { x.Id, name = x.TheType }).ToListAsync(cancellationToken), genders = await _db.Genders.AsNoTracking().Select(x => new { x.Id, name = x.TheType }).ToListAsync(cancellationToken), stages = await _db.StageClasses.AsNoTracking().Select(x => new { x.Id, name = x.NameStage, x.MinClass, x.MaxClass }).ToListAsync(cancellationToken) });
    }

    [HttpGet("school-name-similarity")]
    public async Task<IActionResult> SchoolNameSimilarity(Guid directorateId, string name, CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (!await _db.Directorates.AnyAsync(x => x.Id == directorateId && (!ministryId.HasValue || x.MinistryId == ministryId), cancellationToken)) return Forbid();
        var names = await _db.Schools.AsNoTracking().Where(x => x.DirectorateId == directorateId && !x.IsDeleted).Select(x => x.Name).ToListAsync(cancellationToken);
        var best = names.Select(x => new { name = x, similarity = Similarity(name, x) }).OrderByDescending(x => x.similarity).FirstOrDefault();
        return Ok(best ?? new { name = string.Empty, similarity = 0 });
    }

    [HttpPost("schools")]
    public async Task<IActionResult> CreateSchool(MinistrySchoolRequest request, CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (!request.DirectorateId.HasValue || !await _db.Directorates.AnyAsync(x => x.Id == request.DirectorateId && x.IsActive && (!ministryId.HasValue || x.MinistryId == ministryId), cancellationToken)) return BadRequest(new { message = "المديرية المحددة غير متاحة." });
        if (request.MinClass.HasValue && request.MaxClass.HasValue && request.MinClass > request.MaxClass) return BadRequest(new { message = "الصف الأعلى يجب ألا يقل عن الصف الأدنى." });
        var names = await _db.Schools.Where(x => x.DirectorateId == request.DirectorateId && !x.IsDeleted).Select(x => x.Name).ToListAsync(cancellationToken);
        var maximum = names.Select(x => Similarity(request.Name, x)).DefaultIfEmpty(0).Max();
        if (maximum >= 85) return Conflict(new { message = $"يوجد اسم مدرسة مشابه بنسبة {maximum}٪ داخل المديرية." });
        var school = new School { Id = Guid.NewGuid(), DirectorateId = request.DirectorateId.Value, Name = request.Name.Trim(), IsActive = true, IdStatusSchool = request.IdStatusSchool, IdGender = request.IdGender, IdStage = request.IdStage, MinClass = request.MinClass, MaxClass = request.MaxClass };
        _db.Schools.Add(school); await _db.SaveChangesAsync(cancellationToken); return Created(string.Empty, new { school.Id });
    }

    [HttpPost("directorates")]
    public async Task<IActionResult> CreateDirectorate(MinistryDirectorateRequest request, CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (!ministryId.HasValue) return BadRequest(new { message = "لا يمكن تحديد الوزارة المرتبطة بالحساب الحالي." });
        var code = request.Code.Trim().ToUpperInvariant();
        var name = request.Name.Trim();
        if (await _db.Directorates.AnyAsync(x => x.Code == code, cancellationToken)) return Conflict(new { message = "رمز المديرية مستخدم مسبقًا." });
        if (await _db.Directorates.AnyAsync(x => x.MinistryId == ministryId && x.Name == name, cancellationToken)) return Conflict(new { message = "اسم المديرية مستخدم مسبقًا داخل الوزارة." });
        var directorate = new Directorate { Id = Guid.NewGuid(), MinistryId = ministryId.Value, Code = code, Name = name, City = request.City?.Trim(), Area = request.Area?.Trim(), Phone = request.Phone?.Trim(), Email = request.Email?.Trim(), IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _db.Directorates.Add(directorate);
        await _db.SaveChangesAsync(cancellationToken);
        return Created(string.Empty, new { directorate.Id });
    }
    [HttpPost("people/{type}")]
    public async Task<IActionResult> CreatePerson(string type, MinistryPersonRequest request, CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (!int.TryParse(request.IdNumber, out var idNumber)) return BadRequest(new { message = "رقم الهوية غير صالح." });
        var existingTeacher = type == "teacher" ? await _db.Teachers.SingleOrDefaultAsync(x => x.IdNumber == idNumber, cancellationToken) : null;
        if (await _db.Menegars.AnyAsync(x => x.IdNumber == idNumber, cancellationToken) || await _db.Students.AnyAsync(x => x.IdNumber == idNumber, cancellationToken) || await _db.DirectorateManagers.AnyAsync(x => x.IdNumber == idNumber, cancellationToken) || (existingTeacher != null && !existingTeacher.IsDeleted && !existingTeacher.IsDeletedSchool)) return Conflict(new { message = "رقم الهوية مستخدم مسبقًا." });
        var email = request.Email.Trim();
        if (await _db.Menegars.AnyAsync(x => x.Email == email && !x.IsDeleted, cancellationToken) || await _db.Students.AnyAsync(x => x.Email == email && !x.IsDeletedStudent, cancellationToken) || await _db.DirectorateManagers.AnyAsync(x => x.Email == email && !x.IsDeleted, cancellationToken) || await _db.Teachers.AnyAsync(x => x.Email == email && !x.IsDeleted && (existingTeacher == null || x.Id != existingTeacher.Id), cancellationToken)) return Conflict(new { message = "البريد الإلكتروني مستخدم مسبقًا." });
        if (!request.BirthDate.HasValue) return BadRequest(new { message = "تاريخ الميلاد مطلوب." });
        var today = DateOnly.FromDateTime(DateTime.Today); var age = today.Year - request.BirthDate.Value.Year; if (request.BirthDate.Value > today.AddYears(-age)) age--;
        if (request.BirthDate > today || (type == "student" ? age < 5 : age < 18 || age >= 65)) return BadRequest(new { message = type == "student" ? "يجب ألا يقل عمر الطالب عن 5 سنوات." : "يجب أن يكون العمر بين 18 و64 سنة." });
        if (type == "directorateManager")
        {
            var directorate = await _db.Directorates.Include(x => x.Manager).SingleOrDefaultAsync(x => x.Id == request.OrganizationId && (!ministryId.HasValue || x.MinistryId == ministryId), cancellationToken);
            if (directorate is null || (directorate.Manager != null && !directorate.Manager.IsDeleted)) return Conflict(new { message = "المديرية غير متاحة أو لديها مسؤول." });
            _db.DirectorateManagers.Add(new DirectorateManager { Id = Guid.NewGuid(), DirectorateId = directorate.Id, Name = request.Name.Trim(), IdNumber = idNumber, Phone = request.Phone.Trim(), Email = request.Email.Trim(), City = request.City.Trim(), Area = request.Area.Trim() });
        }
        else
        {
            var school = await _db.Schools.SingleOrDefaultAsync(x => x.Id == request.OrganizationId && x.IsActive && !x.IsDeleted && (!ministryId.HasValue || x.Directorate.MinistryId == ministryId), cancellationToken);
            if (school is null) return BadRequest(new { message = "المدرسة المحددة غير متاحة." });
            if (type == "schoolManager") { if (await _db.Menegars.AnyAsync(x => x.IdSchool == school.Id && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken)) return Conflict(new { message = "المدرسة لديها مدير بالفعل." }); var x = new Menegar { Id = Guid.NewGuid(), Name = request.Name.Trim(), IdNumber = idNumber, Phone = request.Phone.Trim(), Email = request.Email.Trim(), TheDate = request.BirthDate, City = request.City.Trim(), Area = request.Area.Trim(), IdSchool = school.Id }; _db.Menegars.Add(x); _db.SchoolManagerAssignments.Add(new SchoolManagerAssignment { Id = Guid.NewGuid(), ManagerId = x.Id, SchoolId = school.Id, IsPrimary = true, IsActive = true }); }
            else if (type == "teacher") { var x = existingTeacher ?? new Teacher { Id = Guid.NewGuid(), IdNumber = idNumber }; x.Name = request.Name.Trim(); x.Phone = request.Phone.Trim(); x.Email = request.Email.Trim(); x.TheDate = request.BirthDate; x.City = request.City.Trim(); x.Area = request.Area.Trim(); x.IdSchool = school.Id; x.IsDeleted = false; x.IsDeletedSchool = false; if (existingTeacher is null) _db.Teachers.Add(x); else { var oldPlacements = await _db.TeacherPlacements.Where(p => p.TeacherId == x.Id && p.IsActive && p.IsPrimary).ToListAsync(cancellationToken); foreach (var p in oldPlacements) { p.IsActive = false; p.IsPrimary = false; p.EndedAtUtc = DateTime.UtcNow; } if (x.ApplicationUserId.HasValue) { var account = await _db.Users.FindAsync(new object[] { x.ApplicationUserId.Value }, cancellationToken); if (account is not null) account.IsActive = true; } } _db.TeacherPlacements.Add(new TeacherPlacement { Id = Guid.NewGuid(), TeacherId = x.Id, SchoolId = school.Id, IsPrimary = true, IsActive = true }); }
            else if (type == "student") { if (!request.ClassId.HasValue || !await _db.TheClasses.AnyAsync(x => x.Id == request.ClassId && x.IdSchool == school.Id && !x.IsDeleted, cancellationToken)) return BadRequest(new { message = "الصف المحدد غير صالح." }); var x = new Student { Id = Guid.NewGuid(), Name = request.Name.Trim(), IdNumber = idNumber, Phone = request.Phone.Trim(), Email = request.Email.Trim(), TheDate = request.BirthDate, City = request.City.Trim(), Area = request.Area.Trim(), IdSchool = school.Id, IdClass = request.ClassId }; _db.Students.Add(x); _db.StudentEnrollments.Add(new StudentEnrollment { Id = Guid.NewGuid(), StudentId = x.Id, SchoolId = school.Id, ClassId = request.ClassId.Value, IsActive = true }); }
            else return BadRequest(new { message = "نوع السجل غير صالح." });
        }
        await _db.SaveChangesAsync(cancellationToken); return Ok(new { message = "تمت الإضافة بنجاح." });
    }

    [HttpGet("directory/{type}")]
    public async Task<IActionResult> Directory(string type, CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (!ministryId.HasValue && !User.IsInRole(RoleNames.Admin)) return Forbid();
        var schoolIds = _db.Schools.Where(x => !x.IsDeleted && (!ministryId.HasValue || x.Directorate.MinistryId == ministryId)).Select(x => x.Id);
        return type switch
        {
            "schools" or "activeSchools" => Ok(await _db.Schools.AsNoTracking().Where(x => schoolIds.Contains(x.Id) && (type != "activeSchools" || x.IsActive)).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, directorate = x.Directorate.Name, stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : "-", managers = x.Menegars.Count(m => !m.IsDeleted), teachers = x.Teachers.Count(t => !t.IsDeleted), students = x.Students.Count(st => !st.IsDeletedStudent), x.IsActive }).ToListAsync(cancellationToken)),
            "directorateManagers" => Ok(await _db.DirectorateManagers.AsNoTracking().Where(x => !x.IsDeleted && (!ministryId.HasValue || x.Directorate.MinistryId == ministryId)).OrderBy(x => x.Name).Select(x => new { x.Name, directorate = x.Directorate.Name, x.Email, x.Phone, isActive = x.ApplicationUser != null && x.ApplicationUser.IsActive }).ToListAsync(cancellationToken)),
            "schoolManagers" => Ok(await _db.Menegars.AsNoTracking().Where(x => !x.IsDeleted && x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value)).OrderBy(x => x.Name).Select(x => new { x.Name, school = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Name : "-", directorate = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Directorate.Name : "-", x.Email, x.Phone, isActive = x.ApplicationUser != null && x.ApplicationUser.IsActive }).ToListAsync(cancellationToken)),
            "teachers" => Ok(await _db.Teachers.AsNoTracking().Where(x => !x.IsDeleted && x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value)).OrderBy(x => x.Name).Select(x => new { x.Name, school = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Name : "-", directorate = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Directorate.Name : "-", x.Email, x.Phone, isActive = x.ApplicationUser != null && x.ApplicationUser.IsActive }).ToListAsync(cancellationToken)),
            "students" => Ok(await _db.Students.AsNoTracking().Where(x => !x.IsDeletedStudent && x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value)).OrderBy(x => x.Name).Select(x => new { x.Name, school = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Name : "-", directorate = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Directorate.Name : "-", className = x.IdClassNavigation != null ? x.IdClassNavigation.Name : "-", x.Email, x.Phone, isActive = x.ApplicationUser != null && x.ApplicationUser.IsActive }).ToListAsync(cancellationToken)),
            _ => BadRequest(new { message = "نوع القائمة غير صالح." })
        };
    }

    [HttpGet("directorates/{id:guid}")]
    public async Task<IActionResult> Directorate(Guid id, CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (User.IsInRole(RoleNames.MinistryManager) && !ministryId.HasValue) return Forbid();
        var directorate = await _db.Directorates.AsNoTracking().Where(x => x.Id == id && (!ministryId.HasValue || x.MinistryId == ministryId)).Select(x => new
        {
            x.Id, x.Code, x.Name, x.City, x.Area, x.Phone, x.Email, x.IsActive, ministry = x.Ministry.Name, ministryCode = x.Ministry.Code, x.CreatedAtUtc, x.UpdatedAtUtc,
            manager = x.Manager == null || x.Manager.IsDeleted ? null : new { x.Manager.Id, x.Manager.Name, x.Manager.Email, x.Manager.Phone, x.Manager.City, x.Manager.Area, accountActive = x.Manager.ApplicationUser != null && x.Manager.ApplicationUser.IsActive }
        }).SingleOrDefaultAsync(cancellationToken);
        if (directorate is null) return NotFound(new { message = "المديرية غير موجودة." });
        var schools = await _db.Schools.AsNoTracking().Where(x => x.DirectorateId == id && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.IsActive, stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : null, managers = x.Menegars.Count(m => !m.IsDeleted), teachers = x.Teachers.Count(t => !t.IsDeleted), students = x.Students.Count(s => !s.IsDeletedStudent), classes = x.TheClasses.Count(c => !c.IsDeleted) }).ToListAsync(cancellationToken);
        return Ok(new { directorate, summary = new { schools = schools.Count, activeSchools = schools.Count(x => x.IsActive), managers = schools.Sum(x => x.managers), teachers = schools.Sum(x => x.teachers), students = schools.Sum(x => x.students), classes = schools.Sum(x => x.classes) }, schools });
    }

    [HttpPatch("directorates/{id:guid}/activation")]
    public async Task<IActionResult> SetActivation(Guid id, MinistryActivationRequest request, CancellationToken cancellationToken)
    {
        var ministryId = await MinistryScopeAsync(cancellationToken);
        if (User.IsInRole(RoleNames.MinistryManager) && !ministryId.HasValue) return Forbid();
        var directorate = await _db.Directorates.SingleOrDefaultAsync(x => x.Id == id && (!ministryId.HasValue || x.MinistryId == ministryId), cancellationToken);
        if (directorate is null) return NotFound(new { message = "المديرية غير موجودة." });
        directorate.IsActive = request.IsActive;
        directorate.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
    private async Task<Guid?> MinistryScopeAsync(CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.Admin)) return null;
        var user = await _users.GetUserAsync(User);
        if (user is null) return null;
        return await _db.MinistryManagers.AsNoTracking()
            .Where(x => x.ApplicationUserId == user.Id && !x.IsDeleted && x.Ministry.IsActive)
            .Select(x => (Guid?)x.MinistryId).SingleOrDefaultAsync(cancellationToken);
    }
    private static int Similarity(string first, string second)
    {
        first = first.Trim().ToLowerInvariant(); second = second.Trim().ToLowerInvariant();
        if (first == second) return 100; if (first.Length == 0 || second.Length == 0) return 0;
        var previous = Enumerable.Range(0, second.Length + 1).ToArray();
        for (var i = 1; i <= first.Length; i++) { var current = new int[second.Length + 1]; current[0] = i; for (var j = 1; j <= second.Length; j++) current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (first[i - 1] == second[j - 1] ? 0 : 1)); previous = current; }
        return (int)Math.Round((1d - previous[second.Length] / (double)Math.Max(first.Length, second.Length)) * 100);
    }
}
