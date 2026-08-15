using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Model;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    [ApiController]
    [Route("api/Grades")]
    public class GradesApiController : Controller
    {
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly SystemSchoolDbContext _context;
        private readonly ISessionValidatorService _sessionValidatorService;


        public GradesApiController(SystemSchoolDbContext context, INotyfService notyf, IErrorLoggerService logger, ISessionValidatorService sessionValidatorService)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
        }
        // GET: Grades
        [HttpGet("teacher-records")]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> DataGrades(
            Guid? teacherId,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {

            Guid Id;

            try
            {
                Id = teacherId ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }
            
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdTeacher, IdSchool, status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, Id, "Grades/DataGrades");
                if (!IsValid)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }

                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;
                length = Math.Min(length, 100);
                start = Math.Max(start, 0);

                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var baseQuery = _context.Grades.Where(std =>
                    std.IdSchool == IdSchool && std.IdTeacher == IdTeacher &&
                    !std.IsDeletedGrades && !std.IsDeletedTeacher && !std.IsDeletedSchool);

                var totalRecords = await baseQuery.CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = baseQuery
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.GradesId,
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "Unknown",
                        idStudent = s.IdStudent,
                        StudentName = s.IdStudentNavigation == null 
                        ? "فارغ" 
                        : s.IdStudentNavigation.IsDeletedStudent == true
                            ? s.IdStudentNavigation.Name + " (طالب محذوف)" 
                            : s.IdStudentNavigation.Name,
                        ClassroomName = s.IdClassNavigation == null 
                        ? "فارغ" 
                        : s.IdClassNavigation.IsDeleted == true
                            ? s.IdClassNavigation.Name + " (صف محذوف)" 
                            : s.IdClassNavigation.Name,
                        LectuerName = s.IdLectuerNavigation == null 
                        ? "فارغ" 
                        : s.IdLectuerNavigation.IsDeleted == true
                            ? s.IdLectuerNavigation.Name + " (مادة محذوفة)" 
                            : s.IdLectuerNavigation.Name,
                        idClass = s.IdClass,
                        idTeacher = s.IdTeacher,
                        f_m = s.FirstMonth,
                        s_m = s.SecondMonth,
                        mid = s.Mid,
                        Act = s.Activity,
                        final = s.Final,
                        total = s.Total

                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.StudentName != null && s.StudentName.Contains(searchValue)) ||
                        (s.StudentName != null && s.LectuerName.Contains(searchValue)) ||
                        (s.StudentName != null && s.ClassroomName.Contains(searchValue))
                    );
                }

                // عدد السجلات الكلي التي تنطبق عليها الشروط
                var filteredCount = string.IsNullOrWhiteSpace(searchValue)
                    ? totalRecords
                    : await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.StudentName),
                    ("0", "desc") => query.OrderByDescending(s => s.StudentName),
                    ("1", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("1", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    ("2", "asc") => query.OrderBy(s => s.LectuerName),
                    ("2", "desc") => query.OrderByDescending(s => s.LectuerName),
                    _ => query.OrderBy(s => s.StudentName)
                };

                // التقطيع (Pagination)
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // الحصول على البيانات للعرض
                var students = data.
                Select(s => new GradesViewModel
                {
                    Id = s.Id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    IdClass = s.idClass ?? Guid.Empty,
                    IdStudent = s.idStudent ?? Guid.Empty,
                    LectuerName = s.LectuerName,
                    IdTeacher = s.idTeacher ?? Guid.Empty,
                    FirstMonth = s.f_m,
                    SecondMonth = s.s_m,
                    Mid = s.mid,
                    Activity = s.Act,
                    Final = s.final,
                    Total = s.total
                })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = students
                };

                return Json(result);
            }
            catch (Exception e)
            {
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                await _logger.LogAsync(e, "Grades/DataGrades");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Unable to load grade records." });
            }
        }


        
        [HttpGet("student-records")]
        [AuthorizeRoles(RoleNames.Student, RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> DataGradesStudent(
            Guid studentid,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdStudent, IdSchool,status) = await _sessionValidatorService.ValidateStudentDataAccessAsync(HttpContext, studentid, "GradesApi/StudentRecords");
                if (!IsValid)
                {
                    return Json(new { success = false, status= status, error = "Unauthorized access. Session expired." });
                }
                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;
                length = Math.Min(length, 100);
                start = Math.Max(start, 0);

                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var baseQuery = _context.Grades.Where(std =>
                    std.IdSchool == IdSchool && std.IdStudent == IdStudent &&
                    !std.IsDeletedGrades && !std.IsDeletedStudent && !std.IsDeletedSchool);

                var totalRecords = await baseQuery.CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = baseQuery
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.GradesId,
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "Unknown",
                        idStudent = s.IdStudent,
                        StudentName = s.IdStudentNavigation != null ? s.IdStudentNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        idClass = s.IdClass,
                        idTeacher = s.IdTeacher,
                        f_m = s.FirstMonth,
                        s_m = s.SecondMonth,
                        mid = s.Mid,
                        Act = s.Activity,
                        final = s.Final,
                        total = s.Total
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.StudentName != null && s.StudentName.Contains(searchValue)) ||
                        (s.LectuerName != null && s.LectuerName.Contains(searchValue)) ||
                        (s.ClassroomName != null && s.ClassroomName.Contains(searchValue))
                    );
                }

                // الحصول على القيم بعد الفلترة
                var filteredCount = string.IsNullOrWhiteSpace(searchValue)
                    ? totalRecords
                    : await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.StudentName),
                    ("0", "desc") => query.OrderByDescending(s => s.StudentName),
                    ("1", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("1", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    ("2", "asc") => query.OrderBy(s => s.LectuerName),
                    ("2", "desc") => query.OrderByDescending(s => s.LectuerName),
                    _ => query.OrderBy(s => s.StudentName)
                };

                // التقطيع (Pagination)
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();
                
                // الحصول على البيانات للعرض
                var students = data.
                Select(s => new GradesViewModel
                {
                    Id = s.Id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    IdClass = s.idClass ?? Guid.Empty,
                    IdStudent = s.idStudent ?? Guid.Empty,
                    LectuerName = s.LectuerName,
                    IdTeacher = s.idTeacher ?? Guid.Empty,
                    FirstMonth = s.f_m,
                    SecondMonth = s.s_m,
                    Mid = s.mid,
                    Activity = s.Act,
                    Final = s.final,
                    Total = s.total
                    })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = students
                };
                
                return Json(result);
            }
            catch (Exception e)
            {
                await _logger.LogAsync(e, "Grades/DataGradesStudent");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Unable to load student grade records." });
            }
        }

        [HttpGet("subjects")]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> GetSubjectsForTeacher(Guid? teacherId)
        {
            if (!teacherId.HasValue || teacherId.Value == Guid.Empty)
                return BadRequest(new { error = "A valid teacher id is required." });

            var (isValid, validatedTeacherId, schoolId, _) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, teacherId.Value, "GradesApi/Subjects");
            if (!isValid)
                return Forbid();

            var subjects = await _context.TeacherLectuerClasses.AsNoTracking()
                .Where(item => item.IdTeacher == validatedTeacherId && item.IdSchool == schoolId &&
                    !item.IsDeletedTeacherLectuerClass && !item.IsDeletedTeacher &&
                    !item.IsDeletedLectuer && !item.IsDeletedSchool &&
                    !item.IsTeacherRemovedFromLectuer)
                .Select(item => new
                {
                    id = item.IdLectuer ?? Guid.Empty,
                    name = item.IdLectuerNavigation != null ? item.IdLectuerNavigation.Name : "Unknown"
                })
                .Where(item => item.id != Guid.Empty)
                .Distinct()
                .ToListAsync();

            return Ok(subjects);
        }

        [HttpGet("classes")]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> GetClassesForSubject(Guid? teacherId, Guid subjectId)
        {
            if (!teacherId.HasValue || teacherId.Value == Guid.Empty || subjectId == Guid.Empty)
                return BadRequest(new { error = "Valid teacher and subject ids are required." });

            var (isValid, validatedTeacherId, schoolId, _) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, teacherId.Value, "GradesApi/Classes");
            if (!isValid)
                return Forbid();

            var classes = await _context.TeacherLectuerClasses.AsNoTracking()
                .Where(item => item.IdTeacher == validatedTeacherId && item.IdSchool == schoolId &&
                    item.IdLectuer == subjectId && !item.IsDeletedTeacherLectuerClass &&
                    !item.IsDeletedTeacher && !item.IsDeletedLectuer && !item.IsDeletedClass &&
                    !item.IsDeletedSchool && !item.IsTeacherRemovedFromClass &&
                    !item.IsTeacherRemovedFromLectuer)
                .Select(item => new
                {
                    id = item.IdClass ?? Guid.Empty,
                    name = item.IdClassNavigation != null ? item.IdClassNavigation.Name : "Unknown"
                })
                .Where(item => item.id != Guid.Empty)
                .Distinct()
                .ToListAsync();

            return Ok(classes);
        }

        [HttpPost("records")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> SaveRecords([FromBody] GradeBatchRequest request)
        {
            try
            {
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, request.TeacherId, "GradesApi/SaveRecords");
                if (!isValid)
                    return Forbid();

                if (request.LectuerId == Guid.Empty || request.ClassId == Guid.Empty ||
                    request.Items.Count == 0 || request.Items.Select(x => x.StudentId).Distinct().Count() != request.Items.Count)
                    return BadRequest(new { message = "بيانات العلامات غير صالحة." });

                if (!await TeacherOwnsAssignmentAsync(teacherId, schoolId, request.LectuerId, request.ClassId))
                    return Forbid();

                var studentIds = request.Items.Select(x => x.StudentId).ToList();
                var allowedStudents = await _context.StudentLectuerTeachers.AsNoTracking()
                    .Where(x => x.IdTeacher == teacherId && x.IdSchool == schoolId &&
                        x.IdLectuer == request.LectuerId && x.IdClass == request.ClassId &&
                        x.IdStudent.HasValue && studentIds.Contains(x.IdStudent.Value) &&
                        !x.IsDeletedStudentLectuerTeacher && !x.IsDeletedStudent &&
                        !x.IsDeletedTeacher && !x.IsDeletedLectuer && !x.IsDeletedClass &&
                        !x.IsDeletedSchool && !x.IsTeacherRemovedFromClass &&
                        !x.IsTeacherRemovedFromLectuer)
                    .Select(x => x.IdStudent!.Value)
                    .Distinct()
                    .ToListAsync();
                if (allowedStudents.Count != studentIds.Count)
                    return Forbid();

                var existing = await _context.Grades
                    .Where(x => x.IdTeacher == teacherId && x.IdSchool == schoolId &&
                        x.IdLectuer == request.LectuerId && x.IdClass == request.ClassId &&
                        x.IdStudent.HasValue && studentIds.Contains(x.IdStudent.Value) &&
                        !x.IsDeletedGrades)
                    .ToDictionaryAsync(x => x.IdStudent!.Value);

                foreach (var item in request.Items)
                {
                    if (!existing.TryGetValue(item.StudentId, out var grade))
                    {
                        grade = new Grade
                        {
                            IdStudent = item.StudentId,
                            IdTeacher = teacherId,
                            IdSchool = schoolId,
                            IdLectuer = request.LectuerId,
                            IdClass = request.ClassId
                        };
                        _context.Grades.Add(grade);
                    }

                    grade.FirstMonth = item.FirstMonth ?? 0;
                    grade.Mid = item.Mid ?? 0;
                    grade.SecondMonth = item.SecondMonth ?? 0;
                    grade.Activity = item.Activity ?? 0;
                    grade.Final = item.Final ?? 0;
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    message = "تم حفظ العلامات بنجاح.",
                    redirectUrl = Url.Action("ViewGrades", "Grades", new { teacherId })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "GradesApi/SaveRecords");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "تعذر حفظ العلامات." });
            }
        }

        [HttpPut("records/{id:guid}")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> UpdateRecord(Guid id, [FromBody] GradeUpdateRequest request)
        {
            try
            {
                var requestedTeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "GradesApi/UpdateRecord");
                if (!isValid)
                    return Forbid();

                var grade = await _context.Grades.FirstOrDefaultAsync(x =>
                    x.GradesId == id && x.IdTeacher == teacherId && x.IdSchool == schoolId &&
                    !x.IsDeletedGrades && !x.IsDeletedTeacher && !x.IsDeletedSchool);
                if (grade == null)
                    return NotFound(new { message = "سجل العلامات غير موجود." });

                if (!grade.IdLectuer.HasValue || !grade.IdClass.HasValue ||
                    !await TeacherOwnsAssignmentAsync(teacherId, schoolId, grade.IdLectuer.Value, grade.IdClass.Value))
                    return Forbid();

                grade.FirstMonth = request.FirstMonth ?? 0;
                grade.Mid = request.Mid ?? 0;
                grade.SecondMonth = request.SecondMonth ?? 0;
                grade.Activity = request.Activity ?? 0;
                grade.Final = request.Final ?? 0;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم تعديل العلامات بنجاح.",
                    redirectUrl = Url.Action("ViewGrades", "Grades", new { teacherId })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "GradesApi/UpdateRecord");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "تعذر تعديل العلامات." });
            }
        }

        [HttpDelete("records/{id:guid}")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> DeleteRecord(Guid id)
        {
            var requestedTeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
            var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "GradesApi/DeleteRecord");
            if (!isValid)
                return Forbid();

            var grade = await _context.Grades.FirstOrDefaultAsync(x =>
                x.GradesId == id && x.IdTeacher == teacherId && x.IdSchool == schoolId &&
                !x.IsDeletedGrades && !x.IsDeletedTeacher && !x.IsDeletedSchool);
            if (grade == null)
                return NotFound(new { message = "سجل العلامات غير موجود." });

            if (!grade.IdLectuer.HasValue || !grade.IdClass.HasValue ||
                !await TeacherOwnsAssignmentAsync(teacherId, schoolId, grade.IdLectuer.Value, grade.IdClass.Value))
                return Forbid();

            _context.Grades.Remove(grade);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                message = "تم حذف سجل العلامات.",
                redirectUrl = Url.Action("ViewGrades", "Grades", new { teacherId })
            });
        }

        private Task<bool> TeacherOwnsAssignmentAsync(Guid teacherId, Guid schoolId, Guid lectuerId, Guid classId)
        {
            return _context.TeacherLectuerClasses.AsNoTracking().AnyAsync(x =>
                x.IdTeacher == teacherId && x.IdSchool == schoolId &&
                x.IdLectuer == lectuerId && x.IdClass == classId &&
                !x.IsDeletedTeacherLectuerClass && !x.IsDeletedTeacher &&
                !x.IsDeletedLectuer && !x.IsDeletedClass && !x.IsDeletedSchool &&
                !x.IsTeacherRemovedFromClass && !x.IsTeacherRemovedFromLectuer);
        }

        private bool GradeExists(Guid id)
        {
            return _context.Grades.Any(e => e.GradesId == id);
        }
    }
}
