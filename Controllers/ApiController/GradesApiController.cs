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
    [Route("api/[controller]")]
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
        [HttpGet]
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

                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.Grades.Where(std => std.IdSchool == IdSchool && std.IdTeacher == IdTeacher)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Grades.Where(std => std.IdSchool == IdSchool && std.IdTeacher == IdTeacher)
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
                var filteredCount = await query.CountAsync();

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
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        
        [HttpGet]
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
                var (IsValid, IdStudent, IdSchool,status) = await _sessionValidatorService.ValidateStudentSessionAsync(HttpContext, studentid, "Grades/DataGradesStudent");
                if (!IsValid)
                {
                    return Json(new { success = false, status= status, error = "Unauthorized access. Session expired." });
                }
                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;

                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.Grades.Where(std => std.IdSchool == IdSchool && std.IdStudent == IdStudent)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Grades.Where(std => std.IdSchool == IdSchool && std.IdStudent == IdStudent)
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
                var filteredCount = await query.CountAsync();

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
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }

        private bool GradeExists(Guid id)
        {
            return _context.Grades.Any(e => e.GradesId == id);
        }
    }
}
