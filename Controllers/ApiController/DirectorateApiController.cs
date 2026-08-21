using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;
using SchoolSystem.Services.Implementations;
using QuestPDF.Fluent;

namespace SchoolSystem.Controllers;

[ApiController]
[Route("api/directorate")]
[AuthorizeRoles(RoleNames.DirectorateManager)]
public sealed class DirectorateApiController : ControllerBase
{
    private readonly SystemSchoolDbContext _db;
    private readonly ISessionValidatorService _sessionValidator;
    public DirectorateApiController(SystemSchoolDbContext db, ISessionValidatorService sessionValidator) { _db = db; _sessionValidator = sessionValidator; }

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/Dashboard"); if (!access.IsValid) return Forbid(); var id = access.DirectorateId;
        var directorate = await _db.Directorates.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.Code, x.Name, x.City, x.Area, x.IsActive }).SingleAsync(cancellationToken);
        var schoolIds = _db.Schools.Where(x => x.DirectorateId == id && !x.IsDeleted).Select(x => x.Id);
        return Ok(new { directorate, schools = await schoolIds.CountAsync(cancellationToken), activeSchools = await _db.Schools.CountAsync(x => x.DirectorateId == id && !x.IsDeleted && x.IsActive, cancellationToken), managers = await _db.Menegars.CountAsync(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeleted, cancellationToken), teachers = await _db.Teachers.CountAsync(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeleted, cancellationToken), students = await _db.Students.CountAsync(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeletedStudent, cancellationToken), classes = await _db.TheClasses.CountAsync(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value), cancellationToken) });
    }

    [HttpGet("directory-options")]
    public async Task<IActionResult> DirectoryOptions(Guid? schoolId, string? personType, CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/DirectoryOptions"); if (!access.IsValid) return Forbid();
        var schoolQuery = _db.Schools.AsNoTracking().Where(x => x.DirectorateId == access.DirectorateId && !x.IsDeleted && x.IsActive);
        if (string.Equals(personType, "manager", StringComparison.OrdinalIgnoreCase)) schoolQuery = schoolQuery.Where(x => !x.Menegars.Any(m => !m.IsDeleted && !m.IsDeletedSchool));
        var schools = await schoolQuery.OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);
        if (!schoolId.HasValue) return Ok(new { schools, classes = Array.Empty<object>() });
        if (!schools.Any(x => x.Id == schoolId.Value)) return Forbid();
        var classes = await _db.TheClasses.AsNoTracking().Where(x => x.IdSchool == schoolId && !x.IsDeleted && !x.IsDeletedSchool).OrderBy(x => x.NumberClass).ThenBy(x => x.Section).Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);
        return Ok(new { schools, classes });
    }

    [HttpPost("managers")]
    public async Task<IActionResult> CreateManager(DirectoratePersonRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidatePersonRequestAsync(request, false, cancellationToken); if (validation.Error is not null) return validation.Error;
        if (await _db.Menegars.AnyAsync(x => x.IdSchool == request.SchoolId && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken)) return Conflict(new { message = "يوجد مدير مسجل لهذه المدرسة بالفعل." });
        var manager = new Menegar { Id = Guid.NewGuid(), Name = request.Name.Trim(), IdNumber = validation.IdNumber, Phone = request.Phone.Trim(), Email = request.Email.Trim(), TheDate = request.BirthDate, City = request.City.Trim(), Area = request.Area.Trim(), IdSchool = request.SchoolId, IsDeleted = false, IsDeletedSchool = false };
        _db.Menegars.Add(manager);
        _db.SchoolManagerAssignments.Add(new SchoolManagerAssignment { Id = Guid.NewGuid(), ManagerId = manager.Id,
            SchoolId = request.SchoolId!.Value, IsPrimary = true, IsActive = true, StartedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(cancellationToken); return Created(string.Empty, new { manager.Id });
    }

    [HttpPost("teachers")]
    public async Task<IActionResult> CreateTeacher(DirectoratePersonRequest request, CancellationToken cancellationToken)
    {
        int.TryParse(request.IdNumber, out var requestedIdentityNumber);
        var existing = requestedIdentityNumber > 0
            ? await _db.Teachers.SingleOrDefaultAsync(x => x.IdNumber == requestedIdentityNumber, cancellationToken)
            : null;
        var validation = await ValidatePersonRequestAsync(request, false, cancellationToken, existing?.Id); if (validation.Error is not null) return validation.Error;
        if (existing is not null)
        {
            if (!existing.IsDeleted && !existing.IsDeletedSchool)
                return Conflict(new { message = "المعلم فعال بالفعل؛ استخدم التكليف أو طلب النقل لإضافة مدرسة أخرى." });
            existing.Name = request.Name.Trim(); existing.Phone = request.Phone.Trim(); existing.Email = request.Email.Trim();
            existing.TheDate = request.BirthDate; existing.City = request.City.Trim(); existing.Area = request.Area.Trim();
            existing.IdSchool = request.SchoolId; existing.IsDeleted = false; existing.IsDeletedSchool = false;
            var activePrimary = await _db.TeacherPlacements.Where(x => x.TeacherId == existing.Id && x.IsActive && x.IsPrimary).ToListAsync(cancellationToken);
            foreach (var placement in activePrimary) { placement.IsPrimary = false; placement.IsActive = false; placement.EndedAtUtc = DateTime.UtcNow; }
            _db.TeacherPlacements.Add(new TeacherPlacement { Id = Guid.NewGuid(), TeacherId = existing.Id,
                SchoolId = request.SchoolId!.Value, IsPrimary = true, IsActive = true, StartedAtUtc = DateTime.UtcNow });
            if (existing.ApplicationUserId.HasValue)
            {
                var account = await _db.Users.FindAsync(new object[] { existing.ApplicationUserId.Value }, cancellationToken);
                if (account is not null) account.IsActive = true;
            }
            await _db.SaveChangesAsync(cancellationToken);
            return Ok(new { existing.Id, reactivated = true, message = "تمت إعادة تفعيل المعلم وربطه بالمدرسة الجديدة." });
        }
        var teacher = new Teacher { Id = Guid.NewGuid(), Name = request.Name.Trim(), IdNumber = validation.IdNumber, Phone = request.Phone.Trim(), Email = request.Email.Trim(), TheDate = request.BirthDate, City = request.City.Trim(), Area = request.Area.Trim(), IdSchool = request.SchoolId, IsDeleted = false, IsDeletedSchool = false };
        _db.Teachers.Add(teacher);
        _db.TeacherPlacements.Add(new TeacherPlacement { Id = Guid.NewGuid(), TeacherId = teacher.Id,
            SchoolId = request.SchoolId!.Value, IsPrimary = true, IsActive = true, StartedAtUtc = DateTime.UtcNow });
        await _db.SaveChangesAsync(cancellationToken); return Created(string.Empty, new { teacher.Id });
    }

    [HttpPost("students")]
    public async Task<IActionResult> CreateStudent(DirectoratePersonRequest request, CancellationToken cancellationToken)
    {
        var validation = await ValidatePersonRequestAsync(request, true, cancellationToken); if (validation.Error is not null) return validation.Error;
        var student = new Student { Id = Guid.NewGuid(), Name = request.Name.Trim(), IdNumber = validation.IdNumber, Phone = request.Phone.Trim(), Email = request.Email.Trim(), TheDate = request.BirthDate, City = request.City.Trim(), Area = request.Area.Trim(), IdSchool = request.SchoolId, IdClass = request.ClassId, IsDeletedStudent = false, IsDeletedClass = false, IsDeletedSchool = false };
        _db.Students.Add(student);
        _db.StudentEnrollments.Add(new StudentEnrollment { Id = Guid.NewGuid(), StudentId = student.Id,
            SchoolId = request.SchoolId!.Value, ClassId = request.ClassId!.Value, IsActive = true, StartedAtUtc = DateTime.UtcNow });
        var assignments = await _db.TeacherLectuerClasses.AsNoTracking().Where(x => x.IdSchool == request.SchoolId && x.IdClass == request.ClassId && !x.IsDeletedTeacherLectuerClass && !x.IsDeletedTeacher && !x.IsDeletedLectuer && !x.IsDeletedClass && !x.IsDeletedSchool).ToListAsync(cancellationToken);
        foreach (var assignment in assignments) _db.StudentLectuerTeachers.Add(new StudentLectuerTeacher { Id = Guid.NewGuid(), IdStudent = student.Id, IdClass = student.IdClass, IdTeacher = assignment.IdTeacher, IdLectuer = assignment.IdLectuer, IdSchool = student.IdSchool, IsDeletedClass = false, IsDeletedLectuer = false, IsDeletedStudent = false, IsDeletedTeacher = false, IsDeletedStudentLectuerTeacher = false });
        await _db.SaveChangesAsync(cancellationToken); return Created(string.Empty, new { student.Id });
    }

    private async Task<(IActionResult? Error, int IdNumber)> ValidatePersonRequestAsync(DirectoratePersonRequest request, bool student, CancellationToken cancellationToken, Guid? ignoredTeacherId = null)
    {
        var access = await AccessAsync("DirectorateApi/ValidatePersonRequest"); if (!access.IsValid) return (Forbid(), 0);
        if (!request.SchoolId.HasValue || !await _db.Schools.AnyAsync(x => x.Id == request.SchoolId && x.DirectorateId == access.DirectorateId && !x.IsDeleted && x.IsActive, cancellationToken)) return (BadRequest(new { message = "المدرسة المحددة غير متاحة." }), 0);
        if (!int.TryParse(request.IdNumber, out var idNumber)) return (BadRequest(new { message = "رقم الهوية غير صالح." }), 0);
        if (await _db.Menegars.AnyAsync(x => x.IdNumber == idNumber, cancellationToken) || await _db.Teachers.AnyAsync(x => x.IdNumber == idNumber && (!ignoredTeacherId.HasValue || x.Id != ignoredTeacherId.Value), cancellationToken) || await _db.Students.AnyAsync(x => x.IdNumber == idNumber, cancellationToken)) return (Conflict(new { message = "رقم الهوية مستخدم مسبقًا." }), 0);
        var email = request.Email.Trim();
        if (await _db.Menegars.AnyAsync(x => x.Email == email && !x.IsDeleted, cancellationToken) || await _db.Teachers.AnyAsync(x => x.Email == email && !x.IsDeleted && (!ignoredTeacherId.HasValue || x.Id != ignoredTeacherId.Value), cancellationToken) || await _db.Students.AnyAsync(x => x.Email == email && !x.IsDeletedStudent, cancellationToken)) return (Conflict(new { message = "البريد الإلكتروني مستخدم مسبقًا." }), 0);
        if (!request.BirthDate.HasValue) return (BadRequest(new { message = "تاريخ الميلاد مطلوب." }), 0);
        var today = DateOnly.FromDateTime(DateTime.Today); var age = today.Year - request.BirthDate.Value.Year; if (request.BirthDate.Value > today.AddYears(-age)) age--;
        if (request.BirthDate > today || (student ? age < 5 : age < 18 || age >= 65)) return (BadRequest(new { message = student ? "يجب ألا يقل عمر الطالب عن 5 سنوات." : "يجب أن يكون العمر بين 18 و64 سنة." }), 0);
        if (student && (!request.ClassId.HasValue || !await _db.TheClasses.AnyAsync(x => x.Id == request.ClassId && x.IdSchool == request.SchoolId && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken))) return (BadRequest(new { message = "الصف المحدد لا يتبع المدرسة المختارة." }), 0);
        return (null, idNumber);
    }
    [HttpGet("active-schools")]
    public async Task<IActionResult> ActiveSchools(CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/ActiveSchools"); if (!access.IsValid) return Forbid();
        return Ok(await _db.Schools.AsNoTracking().Where(x => x.DirectorateId == access.DirectorateId && !x.IsDeleted && x.IsActive).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : null, managers = x.Menegars.Count(m => !m.IsDeleted), teachers = x.Teachers.Count(t => !t.IsDeleted), students = x.Students.Count(s => !s.IsDeletedStudent) }).ToListAsync(cancellationToken));
    }

    [HttpGet("managers")]
    public async Task<IActionResult> Managers(CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/Managers"); if (!access.IsValid) return Forbid();
        var schoolIds = _db.Schools.Where(x => x.DirectorateId == access.DirectorateId && !x.IsDeleted).Select(x => x.Id);
        return Ok(await _db.Menegars.AsNoTracking().Where(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeleted && !x.IsDeletedSchool).OrderBy(x => x.Name).Select(x => new { x.Id, name = x.Name ?? "-", school = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Name : "-", email = x.Email ?? "-", phone = x.Phone ?? "-", date = x.TheDate, isActive = x.ApplicationUser != null && x.ApplicationUser.IsActive }).ToListAsync(cancellationToken));
    }

    [HttpGet("teachers")]
    public async Task<IActionResult> Teachers(CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/Teachers"); if (!access.IsValid) return Forbid();
        var schoolIds = _db.Schools.Where(x => x.DirectorateId == access.DirectorateId && !x.IsDeleted).Select(x => x.Id);
        return Ok(await _db.Teachers.AsNoTracking().Where(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeleted && !x.IsDeletedSchool).OrderBy(x => x.Name).Select(x => new { x.Id, name = x.Name ?? "-", school = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Name : "-", email = x.Email ?? "-", phone = x.Phone ?? "-", subjects = x.TeacherLectuerClasses.Where(a => !a.IsDeletedTeacherLectuerClass && !a.IsDeletedLectuer).Select(a => a.IdLectuer).Distinct().Count(), classes = x.TeacherLectuerClasses.Where(a => !a.IsDeletedTeacherLectuerClass && !a.IsDeletedClass).Select(a => a.IdClass).Distinct().Count(), isActive = x.ApplicationUser != null && x.ApplicationUser.IsActive }).ToListAsync(cancellationToken));
    }

    [HttpGet("students")]
    public async Task<IActionResult> Students(CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/Students"); if (!access.IsValid) return Forbid();
        var schoolIds = _db.Schools.Where(x => x.DirectorateId == access.DirectorateId && !x.IsDeleted).Select(x => x.Id);
        return Ok(await _db.Students.AsNoTracking().Where(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeletedStudent && !x.IsDeletedSchool).OrderBy(x => x.Name).Select(x => new { x.Id, name = x.Name ?? "-", school = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Name : "-", className = x.IdClassNavigation != null && !x.IsDeletedClass ? x.IdClassNavigation.Name : "غير مسجل", email = x.Email ?? "-", phone = x.Phone ?? "-", date = x.TheDate, isActive = x.ApplicationUser != null && x.ApplicationUser.IsActive }).ToListAsync(cancellationToken));
    }

    [HttpGet("classes")]
    public async Task<IActionResult> Classes(CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/Classes"); if (!access.IsValid) return Forbid();
        var schoolIds = _db.Schools.Where(x => x.DirectorateId == access.DirectorateId && !x.IsDeleted).Select(x => x.Id);
        return Ok(await _db.TheClasses.AsNoTracking().Where(x => x.IdSchool.HasValue && schoolIds.Contains(x.IdSchool.Value) && !x.IsDeleted && !x.IsDeletedSchool).OrderBy(x => x.IdSchoolNavigation!.Name).ThenBy(x => x.NumberClass).ThenBy(x => x.Section).Select(x => new { x.Id, x.Name, school = x.IdSchoolNavigation != null ? x.IdSchoolNavigation.Name : "-", stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : "-", numberClass = x.NumberClass, section = x.Section, branch = x.IdBranchNavigation != null ? x.IdBranchNavigation.BranchName : "-", students = x.Students.Count(s => !s.IsDeletedStudent && !s.IsDeletedClass), teachers = x.TeacherLectuerClasses.Where(a => !a.IsDeletedTeacherLectuerClass && !a.IsDeletedTeacher).Select(a => a.IdTeacher).Distinct().Count() }).ToListAsync(cancellationToken));
    }
    [HttpGet("schools")]
    public async Task<IActionResult> Schools(CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/Schools"); if (!access.IsValid) return Forbid();
        return Ok(await _db.Schools.AsNoTracking().Where(x => x.DirectorateId == access.DirectorateId && !x.IsDeleted).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name, x.IsActive, x.MinClass, x.MaxClass, stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : null, managers = x.Menegars.Count(m => !m.IsDeleted), teachers = x.Teachers.Count(t => !t.IsDeleted), students = x.Students.Count(s => !s.IsDeletedStudent) }).ToListAsync(cancellationToken));
    }

    [HttpGet("schools/{id:guid}")]
    public async Task<IActionResult> School(Guid id, CancellationToken cancellationToken)
    {
        var access = await _sessionValidator.ValidateDirectorateSchoolAccessAsync(HttpContext, id, "DirectorateApi/School"); if (!access.IsValid) return Forbid();
        return Ok(await _db.Schools.AsNoTracking().Where(x => x.Id == id).Select(x => new { x.Id, x.Name, x.IsActive, x.IdStatusSchool, x.IdGender, x.IdStage, x.MinClass, x.MaxClass }).SingleAsync(cancellationToken));
    }

    [HttpGet("schools/{id:guid}/report")]
    public async Task<IActionResult> SchoolReport(Guid id, CancellationToken cancellationToken)
    {
        var access = await _sessionValidator.ValidateDirectorateSchoolAccessAsync(HttpContext, id, "DirectorateApi/SchoolReport");
        if (!access.IsValid) return Forbid();
        var school = await _db.Schools.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted).Select(x => new { x.Id, x.Name, x.IsActive, status = x.IdStatusSchoolNavigation != null ? x.IdStatusSchoolNavigation.TheType : null, gender = x.IdGenderNavigation != null ? x.IdGenderNavigation.TheType : null, stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : null, x.MinClass, x.MaxClass }).SingleAsync(cancellationToken);
        var attendanceQuery = _db.Attendances.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeletedAttendance && !x.IsDeletedSchool && !x.IsDeletedStudent);
        var gradesQuery = _db.Grades.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeletedGrades && !x.IsDeletedSchool && !x.IsDeletedStudent);
        var attendanceTotal = await attendanceQuery.CountAsync(cancellationToken);
        var present = await attendanceQuery.CountAsync(x => x.AttendanceStatus == "1", cancellationToken);
        var absent = await attendanceQuery.CountAsync(x => x.AttendanceStatus == "0", cancellationToken);
        var excused = await attendanceQuery.CountAsync(x => x.AttendanceStatus == "m", cancellationToken);
        var classes = await _db.TheClasses.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeleted && !x.IsDeletedSchool).OrderBy(x => x.NumberClass).ThenBy(x => x.Section).Select(x => new { x.Id, x.Name, stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : null, x.NumberClass, x.Section, branch = x.IdBranchNavigation != null ? x.IdBranchNavigation.BranchName : null, students = x.Students.Count(s => !s.IsDeletedStudent && !s.IsDeletedClass) }).ToListAsync(cancellationToken);
        var subjectRows = await _db.Lectuers.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeleted && !x.IsDeletedSchool).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);
        var assignmentRows = await _db.TeacherLectuerClasses.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeletedTeacherLectuerClass && !x.IsDeletedLectuer && !x.IsDeletedSchool).Select(x => new { x.IdLectuer, x.IdTeacher, x.IdClass, x.IsDeletedTeacher, x.IsDeletedClass }).ToListAsync(cancellationToken);
        var subjects = subjectRows.Select(subject => new { subject.Id, subject.Name, teachers = assignmentRows.Where(x => x.IdLectuer == subject.Id && !x.IsDeletedTeacher && x.IdTeacher.HasValue).Select(x => x.IdTeacher).Distinct().Count(), classes = assignmentRows.Where(x => x.IdLectuer == subject.Id && !x.IsDeletedClass && x.IdClass.HasValue).Select(x => x.IdClass).Distinct().Count() }).ToList();
        var gradeTotals = await gradesQuery.Where(x => x.Total.HasValue).Select(x => x.Total!.Value).ToListAsync(cancellationToken);
        var gradeDistribution = new[] { new { label = "أقل من 50", count = gradeTotals.Count(x => x < 50) }, new { label = "من 50 إلى 59", count = gradeTotals.Count(x => x >= 50 && x < 60) }, new { label = "من 60 إلى 69", count = gradeTotals.Count(x => x >= 60 && x < 70) }, new { label = "من 70 إلى 79", count = gradeTotals.Count(x => x >= 70 && x < 80) }, new { label = "من 80 إلى 89", count = gradeTotals.Count(x => x >= 80 && x < 90) }, new { label = "من 90 إلى 100", count = gradeTotals.Count(x => x >= 90) } }.Select(x => new { x.label, x.count, percentage = gradeTotals.Count == 0 ? 0 : Math.Round(x.count * 100d / gradeTotals.Count, 1) }).ToList();
        var averageGrade = gradeTotals.Count == 0 ? (double?)null : Math.Round(gradeTotals.Average(), 1);
        return Ok(new { school, summary = new { managers = await _db.Menegars.CountAsync(x => x.IdSchool == id && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken), teachers = await _db.Teachers.CountAsync(x => x.IdSchool == id && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken), students = await _db.Students.CountAsync(x => x.IdSchool == id && !x.IsDeletedStudent && !x.IsDeletedSchool, cancellationToken), classes = classes.Count, subjects = subjects.Count }, attendance = new { total = attendanceTotal, present, absent, excused, attendanceRate = attendanceTotal == 0 ? (double?)null : Math.Round((present + excused) * 100d / attendanceTotal, 1) }, academic = new { gradeRecords = gradeTotals.Count, averageGrade, distribution = gradeDistribution }, classes, subjects, generatedAt = DateTimeOffset.UtcNow });
    }

    [HttpGet("schools/{id:guid}/report.pdf")]
    public async Task<IActionResult> DownloadSchoolReportPdf(Guid id, CancellationToken cancellationToken)
    {
        var access = await _sessionValidator.ValidateDirectorateSchoolAccessAsync(HttpContext, id, "DirectorateApi/DownloadSchoolReportPdf");
        if (!access.IsValid) return Forbid();

        var school = await _db.Schools.AsNoTracking().Where(x => x.Id == id && !x.IsDeleted).Select(x => new
        {
            x.Name, x.IsActive,
            Status = x.IdStatusSchoolNavigation != null ? x.IdStatusSchoolNavigation.TheType : null,
            Gender = x.IdGenderNavigation != null ? x.IdGenderNavigation.TheType : null,
            Stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : null,
            x.MinClass, x.MaxClass
        }).SingleAsync(cancellationToken);

        var classRows = await _db.TheClasses.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeleted && !x.IsDeletedSchool)
            .OrderBy(x => x.NumberClass).ThenBy(x => x.Section)
            .Select(x => new { x.Name, Stage = x.IdStageNavigation != null ? x.IdStageNavigation.NameStage : null, x.NumberClass, x.Section, Branch = x.IdBranchNavigation != null ? x.IdBranchNavigation.BranchName : null, Students = x.Students.Count(s => !s.IsDeletedStudent && !s.IsDeletedClass) })
            .ToListAsync(cancellationToken);
        var classes = classRows.Select(x => new DirectorateClassRow(x.Name, x.Stage, x.NumberClass, x.Section, x.Branch, x.Students)).ToList();

        var subjectRows = await _db.Lectuers.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeleted && !x.IsDeletedSchool).OrderBy(x => x.Name).Select(x => new { x.Id, x.Name }).ToListAsync(cancellationToken);
        var assignments = await _db.TeacherLectuerClasses.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeletedTeacherLectuerClass && !x.IsDeletedLectuer && !x.IsDeletedSchool).Select(x => new { x.IdLectuer, x.IdTeacher, x.IdClass, x.IsDeletedTeacher, x.IsDeletedClass }).ToListAsync(cancellationToken);
        var subjects = subjectRows.Select(subject => new DirectorateSubjectRow(subject.Name, assignments.Where(x => x.IdLectuer == subject.Id && !x.IsDeletedTeacher && x.IdTeacher.HasValue).Select(x => x.IdTeacher).Distinct().Count(), assignments.Where(x => x.IdLectuer == subject.Id && !x.IsDeletedClass && x.IdClass.HasValue).Select(x => x.IdClass).Distinct().Count())).ToList();

        var attendanceQuery = _db.Attendances.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeletedAttendance && !x.IsDeletedSchool && !x.IsDeletedStudent);
        var attendanceTotal = await attendanceQuery.CountAsync(cancellationToken);
        var present = await attendanceQuery.CountAsync(x => x.AttendanceStatus == "1", cancellationToken);
        var absent = await attendanceQuery.CountAsync(x => x.AttendanceStatus == "0", cancellationToken);
        var excused = await attendanceQuery.CountAsync(x => x.AttendanceStatus == "m", cancellationToken);
        var gradeTotals = await _db.Grades.AsNoTracking().Where(x => x.IdSchool == id && !x.IsDeletedGrades && !x.IsDeletedSchool && !x.IsDeletedStudent && x.Total.HasValue).Select(x => x.Total!.Value).ToListAsync(cancellationToken);
        var rawBuckets = new[] { ("أقل من 50", gradeTotals.Count(x => x < 50)), ("من 50 إلى 59", gradeTotals.Count(x => x >= 50 && x < 60)), ("من 60 إلى 69", gradeTotals.Count(x => x >= 60 && x < 70)), ("من 70 إلى 79", gradeTotals.Count(x => x >= 70 && x < 80)), ("من 80 إلى 89", gradeTotals.Count(x => x >= 80 && x < 90)), ("من 90 إلى 100", gradeTotals.Count(x => x >= 90)) };
        var distribution = rawBuckets.Select(x => new DirectorateGradeBucket(x.Item1, x.Item2, gradeTotals.Count == 0 ? 0 : Math.Round(x.Item2 * 100d / gradeTotals.Count, 1))).ToList();

        var data = new DirectorateSchoolPdfData(school.Name, school.IsActive, school.Status, school.Gender, school.Stage, school.MinClass, school.MaxClass,
            await _db.Menegars.CountAsync(x => x.IdSchool == id && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken),
            await _db.Teachers.CountAsync(x => x.IdSchool == id && !x.IsDeleted && !x.IsDeletedSchool, cancellationToken),
            await _db.Students.CountAsync(x => x.IdSchool == id && !x.IsDeletedStudent && !x.IsDeletedSchool, cancellationToken),
            classes.Count, subjects.Count, attendanceTotal, present, absent, excused,
            attendanceTotal == 0 ? null : Math.Round((present + excused) * 100d / attendanceTotal, 1),
            gradeTotals.Count == 0 ? null : Math.Round(gradeTotals.Average(), 1), distribution, classes, subjects, DateTimeOffset.UtcNow);

        using var stream = new MemoryStream();
        new DirectorateSchoolReportPdf(data).GeneratePdf(stream);
        var safeName = string.Concat(school.Name.Select(character => Path.GetInvalidFileNameChars().Contains(character) ? '_' : character));
        return File(stream.ToArray(), "application/pdf", $"تقرير_{safeName}.pdf");
    }
    [HttpGet("school-options")]
    public async Task<IActionResult> SchoolOptions(CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/SchoolOptions"); if (!access.IsValid) return Forbid();
        return Ok(new { statuses = await _db.StatusSchools.AsNoTracking().Select(x => new { x.Id, name = x.TheType }).ToListAsync(cancellationToken), genders = await _db.Genders.AsNoTracking().Select(x => new { x.Id, name = x.TheType }).ToListAsync(cancellationToken), stages = await _db.StageClasses.AsNoTracking().Select(x => new { x.Id, name = x.NameStage, x.MinClass, x.MaxClass }).ToListAsync(cancellationToken) });
    }

    [HttpPost("schools")]
    public async Task<IActionResult> CreateSchool(DirectorateSchoolRequest request, CancellationToken cancellationToken)
    {
        var access = await AccessAsync("DirectorateApi/CreateSchool"); if (!access.IsValid) return Forbid();
        if (request.MinClass.HasValue && request.MaxClass.HasValue && request.MinClass > request.MaxClass) ModelState.AddModelError(nameof(request.MaxClass), "الصف الأعلى يجب ألا يقل عن الصف الأدنى.");
        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        if (await _db.Schools.AnyAsync(x => x.DirectorateId == access.DirectorateId && x.Name == request.Name.Trim() && !x.IsDeleted, cancellationToken)) return Conflict(new { message = "يوجد مدرسة بالاسم نفسه داخل المديرية." });
        var school = new School { Id = Guid.NewGuid(), DirectorateId = access.DirectorateId, Name = request.Name.Trim(), IsActive = true, IdStatusSchool = request.IdStatusSchool, IdGender = request.IdGender, IdStage = request.IdStage, MinClass = request.MinClass, MaxClass = request.MaxClass };
        _db.Schools.Add(school); await _db.SaveChangesAsync(cancellationToken); return CreatedAtAction(nameof(School), new { id = school.Id }, new { school.Id });
    }

    [HttpPut("schools/{id:guid}")]
    public async Task<IActionResult> UpdateSchool(Guid id, DirectorateSchoolRequest request, CancellationToken cancellationToken)
    {
        var access = await _sessionValidator.ValidateDirectorateSchoolAccessAsync(HttpContext, id, "DirectorateApi/UpdateSchool"); if (!access.IsValid) return Forbid();
        if (request.MinClass.HasValue && request.MaxClass.HasValue && request.MinClass > request.MaxClass) return BadRequest(new { message = "الصف الأعلى يجب ألا يقل عن الصف الأدنى." });
        if (await _db.Schools.AnyAsync(x => x.Id != id && x.DirectorateId == access.DirectorateId && x.Name == request.Name.Trim() && !x.IsDeleted, cancellationToken)) return Conflict(new { message = "يوجد مدرسة بالاسم نفسه داخل المديرية." });
        var school = await _db.Schools.SingleAsync(x => x.Id == id, cancellationToken); school.Name = request.Name.Trim(); school.IdStatusSchool = request.IdStatusSchool; school.IdGender = request.IdGender; school.IdStage = request.IdStage; school.MinClass = request.MinClass; school.MaxClass = request.MaxClass; await _db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    [HttpPatch("schools/{id:guid}/activation")]
    public async Task<IActionResult> SetActivation(Guid id, SchoolActivationRequest request, CancellationToken cancellationToken)
    {
        var access = await _sessionValidator.ValidateDirectorateSchoolAccessAsync(HttpContext, id, "DirectorateApi/SetActivation"); if (!access.IsValid) return Forbid();
        var school = await _db.Schools.SingleAsync(x => x.Id == id, cancellationToken); school.IsActive = request.IsActive; await _db.SaveChangesAsync(cancellationToken); return NoContent();
    }

    private Task<(bool IsValid, Guid DirectorateId, string Message)> AccessAsync(string source) => _sessionValidator.ValidateDirectorateManagerSessionAsync(HttpContext, source);
}
