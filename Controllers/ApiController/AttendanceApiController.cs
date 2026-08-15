using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
    [Route("api/Attendance")]
    public class AttendanceApiController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;
        

        public AttendanceApiController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger, ISessionValidatorService sessionValidatorService)
        {
            _sessionValidatorService = sessionValidatorService;
            _context = context;
            _notyf = notyf;
            _logger = logger;
            _sessionValidatorService = sessionValidatorService;
        }
        // GET: Attendance
        
        [HttpGet("teacher-records")]
        [AuthorizeRoles("Teacher")]
        public async Task<JsonResult> DataAttendance(
            Guid teacherId,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                Console.WriteLine($"Id TTTeacher: {teacherId}");
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, teacherId, "Attendance/DataAttendance");

                if (!IsValid)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }

                //فحص اذا كان تم ارسال قيمة المتغير ام لا و وصع قيمة افتراضية اذا كان لا
                if (length <= 0)
                    length = 10;
                length = Math.Min(length, 100);
                start = Math.Max(start, 0);

                // تحديد قيمة الـ searchValue
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var baseQuery = _context.Attendances.AsNoTracking().Where(std =>
                    std.IdSchool == IdSchool && std.IdTeacher == IdTeacher &&
                    !std.IsDeletedAttendance && !std.IsDeletedStudent &&
                    !std.IsDeletedClass && !std.IsDeletedLectuer &&
                    !std.IsDeletedTeacher && !std.IsDeletedSchool);

                var totalRecords = await baseQuery.CountAsync();

                var filteredQuery = baseQuery;
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    searchValue = searchValue.Trim();
                    var statusValues = new List<string>();
                    if ("حضور".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statusValues.Add("1");
                    if ("غياب".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statusValues.Add("0");
                    if ("غياب بعذر".Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                        "بعذر".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statusValues.Add("m");

                    var hasDate = DateOnly.TryParse(searchValue, out var searchedDate);
                    filteredQuery = statusValues.Count == 0
                        ? filteredQuery.Where(s =>
                            (s.IdStudentNavigation != null && s.IdStudentNavigation.Name.Contains(searchValue)) ||
                            (s.IdLectuerNavigation != null && s.IdLectuerNavigation.Name.Contains(searchValue)) ||
                            (s.IdClassNavigation != null && s.IdClassNavigation.Name.Contains(searchValue)) ||
                            (s.Excuse != null && s.Excuse.Contains(searchValue)) ||
                            (hasDate && s.DateAndTime == searchedDate))
                        : filteredQuery.Where(s =>
                            (s.IdStudentNavigation != null && s.IdStudentNavigation.Name.Contains(searchValue)) ||
                            (s.IdLectuerNavigation != null && s.IdLectuerNavigation.Name.Contains(searchValue)) ||
                            (s.IdClassNavigation != null && s.IdClassNavigation.Name.Contains(searchValue)) ||
                            (s.Excuse != null && s.Excuse.Contains(searchValue)) ||
                            statusValues.Contains(s.AttendanceStatus) ||
                            (hasDate && s.DateAndTime == searchedDate));
                }

                var filteredCount = string.IsNullOrWhiteSpace(searchValue)
                    ? totalRecords
                    : await filteredQuery.CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = filteredQuery
                    .Select(s => new
                    {
                        id = s.Id,
                        StudentName = s.IdStudentNavigation != null ? s.IdStudentNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        excuse = s.Excuse ?? "Null",
                        Date = s.DateAndTime,
                        Status = s.AttendanceStatus
                        
                        
                    });

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.StudentName),
                    ("0", "desc") => query.OrderByDescending(s => s.StudentName),
                    ("1", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("1", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    ("2", "asc") => query.OrderBy(s => s.LectuerName),
                    ("2", "desc") => query.OrderByDescending(s => s.LectuerName),
                    ("3", "asc") => query.OrderBy(s => s.Status),
                    ("3", "desc") => query.OrderByDescending(s => s.Status),
                    ("4", "asc") => query.OrderBy(s => s.Date),
                    ("4", "desc") => query.OrderByDescending(s => s.Date),
                    ("5", "asc") => query.OrderBy(s => s.excuse),
                    ("5", "desc") => query.OrderByDescending(s => s.excuse),
                    _ => query.OrderByDescending(s => s.Date).ThenBy(s => s.StudentName)
                };

                // التقطيع (Pagination)
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // الحصول على البيانات للعرض
                var students = data.
                Select(s => new AttendanceViewModel
                {
                    Id = s.id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    LectuerName = s.LectuerName,
                    DateAndTime = s.Date,
                    Excuse = s.excuse,
                    AttendanceStatus = s.Status
                    })
                    .ToList();

                //ارجاع بيانات العرض اللازمة
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
                // حال كان هناك خطأ غير متوقع
                await _logger.LogAsync(e, "Attendance/DataAttendance");
                return Json(new { error = "Unable to load attendance records." });
            }
        }

        [HttpGet("subjects")]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> GetLectuerForTeacher(Guid teacherId)
        {
            if(teacherId != HttpContext.Session.GetGuid("Id"))
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/GetLectuerForTeacher");
                return Forbid();
            }
            var (isValid, validatedTeacherId, schoolId, _) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, teacherId, "AttendanceApi/Subjects");
            if (!isValid)
                return Forbid();

            var subjects = await _context.TeacherLectuerClasses
                .AsNoTracking()
                .Where(ts => ts.IdTeacher == validatedTeacherId && ts.IdSchool == schoolId &&
                    !ts.IsDeletedTeacherLectuerClass && !ts.IsDeletedTeacher &&
                    !ts.IsDeletedLectuer && !ts.IsDeletedSchool &&
                    !ts.IsTeacherRemovedFromLectuer)
                .Select(ts => new {
                    id = ts.IdLectuerNavigation != null ? ts.IdLectuerNavigation.Id : Guid.Empty,
                    name = ts.IdLectuerNavigation!=null? ts.IdLectuerNavigation.Name:"غير معرف"
                })
                .Where(subject => subject.id != Guid.Empty)
                .Distinct()
                .ToListAsync();

            return Json(subjects);
        }

        [HttpGet("classes")]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> GetClassForSubject(Guid teacherId, Guid subjectId)
        {
            if (teacherId != HttpContext.Session.GetGuid("Id"))
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/GetClassForSubject");
                return Forbid();
            }
            if (subjectId == Guid.Empty)
                return BadRequest(new { error = "A valid subject id is required." });

            var (isValid, validatedTeacherId, schoolId, _) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, teacherId, "AttendanceApi/Classes");
            if (!isValid)
                return Forbid();

            var grades = await _context.TeacherLectuerClasses
                .AsNoTracking()
                .Where(tg => tg.IdTeacher == validatedTeacherId && tg.IdSchool == schoolId &&
                    tg.IdLectuer == subjectId && !tg.IsDeletedTeacherLectuerClass &&
                    !tg.IsDeletedTeacher && !tg.IsDeletedLectuer && !tg.IsDeletedClass &&
                    !tg.IsDeletedSchool && !tg.IsTeacherRemovedFromClass &&
                    !tg.IsTeacherRemovedFromLectuer)
                .Select(tg => new
                {
                    id = tg.IdClassNavigation != null ? tg.IdClassNavigation.Id : Guid.Empty,
                    name = tg.IdClassNavigation != null ? tg.IdClassNavigation.Name : "غير معرف"
                })
                .Where(grade => grade.id != Guid.Empty)
                .Distinct()
                .ToListAsync();

            if (grades == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/GetClassForSubject");
                return NotFound();
            }

            return Json(grades);
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager, RoleNames.Student)]
        [HttpGet("student-summary")]
        public async Task<IActionResult> StudentAttendanceSummary(
            Guid studentid,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                var (isValid, studentId, schoolId, _) = await _sessionValidatorService
                    .ValidateStudentDataAccessAsync(HttpContext, studentid, "AttendanceApi/StudentSummary");
                if (!isValid) return Forbid();

                start = Math.Max(start, 0);
                length = Math.Clamp(length <= 0 ? 10 : length, 1, 100);
                var orderColumn = Request.Query["order[0][column]"].ToString();
                var orderDirection = Request.Query["order[0][dir]"].ToString().ToLowerInvariant();

                var records = _context.Attendances.AsNoTracking().Where(a =>
                    a.IdStudent == studentId && a.IdSchool == schoolId &&
                    a.IdTeacher.HasValue && a.IdLectuer.HasValue && a.DateAndTime.HasValue &&
                    !a.IsDeletedAttendance && !a.IsDeletedStudent && !a.IsDeletedTeacher &&
                    !a.IsDeletedLectuer && !a.IsDeletedSchool &&
                    a.IdTeacherNavigation != null && a.IdLectuerNavigation != null);

                var query = records
                    .GroupBy(a => new
                    {
                        TeacherId = a.IdTeacher!.Value,
                        TeacherName = a.IdTeacherNavigation!.Name,
                        LectuerId = a.IdLectuer!.Value,
                        LectuerName = a.IdLectuerNavigation!.Name
                    })
                    .Select(group => new
                    {
                        teacherId = group.Key.TeacherId,
                        teacherName = group.Key.TeacherName,
                        lectuerId = group.Key.LectuerId,
                        lectuerName = group.Key.LectuerName,
                        attendanceDays = group.Where(a => a.AttendanceStatus == "1")
                            .Select(a => a.DateAndTime).Distinct().Count(),
                        totalDays = group.Select(a => a.DateAndTime).Distinct().Count()
                    });

                var totalRecords = await query.CountAsync();
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    searchValue = searchValue.Trim();
                    query = query.Where(item =>
                        item.lectuerName.Contains(searchValue) ||
                        item.teacherName.Contains(searchValue));
                }

                var filteredRecords = string.IsNullOrWhiteSpace(searchValue)
                    ? totalRecords
                    : await query.CountAsync();

                query = (orderColumn, orderDirection) switch
                {
                    ("0", "desc") => query.OrderByDescending(x => x.lectuerName),
                    ("1", "asc") => query.OrderBy(x => x.teacherName),
                    ("1", "desc") => query.OrderByDescending(x => x.teacherName),
                    ("2", "asc") => query.OrderBy(x => x.attendanceDays).ThenBy(x => x.totalDays),
                    ("2", "desc") => query.OrderByDescending(x => x.attendanceDays).ThenByDescending(x => x.totalDays),
                    _ => query.OrderBy(x => x.lectuerName)
                };

                var data = await query.Skip(start).Take(length).ToListAsync();
                return Ok(new { draw, recordsTotal = totalRecords, recordsFiltered = filteredRecords, data });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "AttendanceApi/StudentSummary");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "تعذر تحميل ملخص حضور الطالب." });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager, RoleNames.Student)]
        [HttpGet("student-details")]
        public async Task<IActionResult> StudentAttendanceDetails(
            Guid studentid,
            Guid teacherId,
            Guid lectuerId,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                var (isValid, studentId, schoolId, _) = await _sessionValidatorService
                    .ValidateStudentDataAccessAsync(HttpContext, studentid, "AttendanceApi/StudentDetails");
                if (!isValid) return Forbid();
                if (teacherId == Guid.Empty || lectuerId == Guid.Empty)
                    return BadRequest(new { message = "المعلم أو المادة غير صالحين." });

                start = Math.Max(start, 0);
                length = Math.Clamp(length <= 0 ? 10 : length, 1, 100);
                var orderColumn = Request.Query["order[0][column]"].ToString();
                var orderDirection = Request.Query["order[0][dir]"].ToString().ToLowerInvariant();

                var query = _context.Attendances.AsNoTracking().Where(a =>
                    a.IdStudent == studentId && a.IdSchool == schoolId &&
                    a.IdTeacher == teacherId && a.IdLectuer == lectuerId &&
                    !a.IsDeletedAttendance && !a.IsDeletedStudent && !a.IsDeletedTeacher &&
                    !a.IsDeletedLectuer && !a.IsDeletedSchool);
                var totalRecords = await query.CountAsync();
                if (totalRecords == 0)
                    return NotFound(new { message = "لا توجد سجلات حضور لهذه المادة والمعلم." });

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    searchValue = searchValue.Trim();
                    var statuses = new List<string>();
                    if ("حضور".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statuses.Add("1");
                    if ("غياب".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statuses.Add("0");
                    if ("غياب بعذر".Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                        "بعذر".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statuses.Add("m");
                    var hasDate = DateOnly.TryParse(searchValue, out var date);

                    query = statuses.Count == 0
                        ? query.Where(a => (a.Excuse != null && a.Excuse.Contains(searchValue)) ||
                            (hasDate && a.DateAndTime == date))
                        : query.Where(a => (a.Excuse != null && a.Excuse.Contains(searchValue)) ||
                            statuses.Contains(a.AttendanceStatus) || (hasDate && a.DateAndTime == date));
                }

                var filteredRecords = string.IsNullOrWhiteSpace(searchValue)
                    ? totalRecords
                    : await query.CountAsync();

                query = (orderColumn, orderDirection) switch
                {
                    ("0", "asc") => query.OrderBy(a => a.DateAndTime),
                    ("1", "asc") => query.OrderBy(a => a.AttendanceStatus),
                    ("1", "desc") => query.OrderByDescending(a => a.AttendanceStatus),
                    ("2", "asc") => query.OrderBy(a => a.Excuse),
                    ("2", "desc") => query.OrderByDescending(a => a.Excuse),
                    _ => query.OrderByDescending(a => a.DateAndTime)
                };

                var data = await query.Skip(start).Take(length)
                    .Select(a => new
                    {
                        id = a.Id,
                        dateAndTime = a.DateAndTime,
                        attendanceStatus = a.AttendanceStatus,
                        excuse = a.Excuse
                    })
                    .ToListAsync();

                return Ok(new { draw, recordsTotal = totalRecords, recordsFiltered = filteredRecords, data });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "AttendanceApi/StudentDetails");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "تعذر تحميل تفاصيل حضور الطالب." });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager, RoleNames.Student)]
        [HttpGet("student-records")]
        public async Task<JsonResult> AttendancesStudentData(
            Guid studentid,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdStudent, IdSchool, status) = await _sessionValidatorService.ValidateStudentDataAccessAsync(HttpContext, studentid, "AttendanceApi/StudentRecords");
                if (!IsValid)
                {
                    return Json(new { success = false, status= status, error = "Unauthorized access. Session expired." });
                }

                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;
                length = Math.Min(length, 100);
                start = Math.Max(start, 0);

                // تحديد قيمة الـ searchValue
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var baseQuery = _context.Attendances.AsNoTracking().Where(std =>
                    std.IdSchool == IdSchool && std.IdStudent == IdStudent &&
                    !std.IsDeletedAttendance && !std.IsDeletedStudent && !std.IsDeletedSchool);

                var totalRecords = await baseQuery.CountAsync();

                var filteredQuery = baseQuery;
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    searchValue = searchValue.Trim();
                    var statusValues = new List<string>();
                    if ("حضور".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statusValues.Add("1");
                    if ("غياب".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statusValues.Add("0");
                    if ("غياب بعذر".Contains(searchValue, StringComparison.OrdinalIgnoreCase) ||
                        "بعذر".Contains(searchValue, StringComparison.OrdinalIgnoreCase)) statusValues.Add("m");
                    var hasDate = DateOnly.TryParse(searchValue, out var searchedDate);
                    filteredQuery = statusValues.Count == 0
                        ? filteredQuery.Where(s =>
                            (s.IdStudentNavigation != null && s.IdStudentNavigation.Name.Contains(searchValue)) ||
                            (s.IdLectuerNavigation != null && s.IdLectuerNavigation.Name.Contains(searchValue)) ||
                            (s.IdClassNavigation != null && s.IdClassNavigation.Name.Contains(searchValue)) ||
                            (s.Excuse != null && s.Excuse.Contains(searchValue)) ||
                            (hasDate && s.DateAndTime == searchedDate))
                        : filteredQuery.Where(s =>
                            (s.IdStudentNavigation != null && s.IdStudentNavigation.Name.Contains(searchValue)) ||
                            (s.IdLectuerNavigation != null && s.IdLectuerNavigation.Name.Contains(searchValue)) ||
                            (s.IdClassNavigation != null && s.IdClassNavigation.Name.Contains(searchValue)) ||
                            (s.Excuse != null && s.Excuse.Contains(searchValue)) ||
                            statusValues.Contains(s.AttendanceStatus) ||
                            (hasDate && s.DateAndTime == searchedDate));
                }

                var filteredCount = string.IsNullOrWhiteSpace(searchValue)
                    ? totalRecords
                    : await filteredQuery.CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = filteredQuery
                    .Select(s => new
                    {
                        id = s.Id,
                        StudentName = s.IdStudentNavigation != null ? s.IdStudentNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        excuse = s.Excuse ?? "Null",
                        Date = s.DateAndTime,
                        Status = s.AttendanceStatus
                        
                        
                    });

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.LectuerName),
                    ("0", "desc") => query.OrderByDescending(s => s.LectuerName),
                    ("1", "asc") => query.OrderBy(s => s.Status),
                    ("1", "desc") => query.OrderByDescending(s => s.Status),
                    ("2", "asc") => query.OrderBy(s => s.Date),
                    ("2", "desc") => query.OrderByDescending(s => s.Date),
                    ("3", "asc") => query.OrderBy(s => s.excuse),
                    ("3", "desc") => query.OrderByDescending(s => s.excuse),
                    _ => query.OrderByDescending(s => s.Date)
                };

                // التقطيع (Pagination)
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                var students = data.
                Select(s => new AttendanceViewModel
                {
                    Id = s.id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    LectuerName = s.LectuerName,
                    DateAndTime = s.Date,
                    Excuse = s.excuse,
                    AttendanceStatus = s.Status
                    })
                    .ToList();

                // ارسال البيانات
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
                // حال كان هناك خطأ غير متوقع
                await _logger.LogAsync(e, "Attendance/AttendancesStudentData");
                return Json(new { error = "Unable to load student attendance records." });
            }
        }

        [HttpPost("records")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> CreateRecords([FromBody] AttendanceBatchRequest request)
        {
            try
            {
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, request.TeacherId, "AttendanceApi/CreateRecords");
                if (!isValid)
                    return Forbid();

                if (request.LectuerId == Guid.Empty || request.ClassId == Guid.Empty ||
                    request.Items.Count == 0 || request.Items.Select(x => x.StudentId).Distinct().Count() != request.Items.Count)
                    return BadRequest(new { message = "بيانات الحضور غير صالحة." });

                if (!await TeacherOwnsAssignmentAsync(teacherId, schoolId, request.LectuerId, request.ClassId))
                    return Forbid();

                var studentIds = request.Items.Select(x => x.StudentId).ToList();
                var allowedStudentIds = await _context.StudentLectuerTeachers.AsNoTracking()
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
                if (allowedStudentIds.Count != studentIds.Count)
                    return Forbid();

                var date = DateOnly.FromDateTime(DateTime.Now);
                var existing = await _context.Attendances
                    .Where(x => x.IdTeacher == teacherId && x.IdSchool == schoolId &&
                        x.IdLectuer == request.LectuerId && x.IdClass == request.ClassId &&
                        x.DateAndTime == date && x.IdStudent.HasValue && studentIds.Contains(x.IdStudent.Value) &&
                        !x.IsDeletedAttendance)
                    .ToDictionaryAsync(x => x.IdStudent!.Value);

                foreach (var item in request.Items)
                {
                    var excuse = item.Status == "m" ? item.Excuse?.Trim() : null;
                    if (existing.TryGetValue(item.StudentId, out var attendance))
                    {
                        attendance.AttendanceStatus = item.Status;
                        attendance.Excuse = excuse;
                    }
                    else
                    {
                        _context.Attendances.Add(new Attendance
                        {
                            IdStudent = item.StudentId,
                            IdTeacher = teacherId,
                            IdSchool = schoolId,
                            IdLectuer = request.LectuerId,
                            IdClass = request.ClassId,
                            DateAndTime = date,
                            AttendanceStatus = item.Status,
                            Excuse = excuse
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new
                {
                    success = true,
                    message = "تم حفظ الحضور والغياب بنجاح.",
                    redirectUrl = Url.Action("ViewAttendance", "Attendance", new { teacherId })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "AttendanceApi/CreateRecords");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "تعذر حفظ الحضور والغياب." });
            }
        }

        [HttpPut("records/{id:guid}")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> UpdateRecord(Guid id, [FromBody] AttendanceUpdateRequest request)
        {
            try
            {
                var requestedTeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "AttendanceApi/UpdateRecord");
                if (!isValid)
                    return Forbid();

                var attendance = await _context.Attendances.FirstOrDefaultAsync(x =>
                    x.Id == id && x.IdTeacher == teacherId && x.IdSchool == schoolId &&
                    !x.IsDeletedAttendance && !x.IsDeletedTeacher && !x.IsDeletedSchool);
                if (attendance == null)
                    return NotFound(new { message = "سجل الحضور غير موجود." });

                if (!attendance.IdLectuer.HasValue || !attendance.IdClass.HasValue ||
                    !await TeacherOwnsAssignmentAsync(teacherId, schoolId, attendance.IdLectuer.Value, attendance.IdClass.Value))
                    return Forbid();

                attendance.AttendanceStatus = request.Status;
                attendance.Excuse = request.Status == "m" ? request.Excuse?.Trim() : null;
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "تم تعديل الحضور بنجاح.",
                    redirectUrl = Url.Action("ViewAttendance", "Attendance", new { teacherId })
                });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "AttendanceApi/UpdateRecord");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "تعذر تعديل سجل الحضور." });
            }
        }

        [HttpDelete("records/{id:guid}")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> DeleteRecord(Guid id)
        {
            var requestedTeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
            var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "AttendanceApi/DeleteRecord");
            if (!isValid)
                return Forbid();

            var attendance = await _context.Attendances.FirstOrDefaultAsync(x =>
                x.Id == id && x.IdTeacher == teacherId && x.IdSchool == schoolId &&
                !x.IsDeletedAttendance && !x.IsDeletedTeacher && !x.IsDeletedSchool);
            if (attendance == null)
                return NotFound(new { message = "سجل الحضور غير موجود." });

            if (!attendance.IdLectuer.HasValue || !attendance.IdClass.HasValue ||
                !await TeacherOwnsAssignmentAsync(teacherId, schoolId, attendance.IdLectuer.Value, attendance.IdClass.Value))
                return Forbid();

            _context.Attendances.Remove(attendance);
            await _context.SaveChangesAsync();
            return Ok(new
            {
                success = true,
                message = "تم حذف سجل الحضور.",
                redirectUrl = Url.Action("ViewAttendance", "Attendance", new { teacherId })
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


        private bool AttendanceExists(Guid id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }
    }
}
