using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Packaging.Signing;
using QuestPDF.Fluent;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    public class TeacherController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;


        public TeacherController(SystemSchoolDbContext context, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
        }

        // GET: Teacher
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Index()
        {
            Guid IdSchool = HttpContext.Session.GetGuid("School") ?? Guid.Empty;
            if(IdSchool != Guid.Empty){
                string Role = HttpContext.Session.GetString("Role")??"null";
                if (Role == "Teacher")
                {
                    Guid Id = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
                    Console.WriteLine($"Id: {Id}");
                    if (Id != Guid.Empty)
                    {
                        Teacher? teacher = await _context.Teachers.Where(t => t.Id == Id && t.IdSchool == IdSchool).FirstOrDefaultAsync();
                        if (teacher != null)
                        {
                            Console.WriteLine($"Id Teacher123: {teacher.Id}");
                            TeacherViewModel teacherViewModel = new TeacherViewModel
                            {
                                Id = teacher.Id
                            };
                            return View(teacherViewModel);
                        }
                    }
                }

            }
                // إذا كان المستخدم مصادقًا عليه بالفعل، قم بإعادة توجيهه إلى الصفحة الرئيسية
                Exception ex = new Exception("Bypass verification system");
                await _logger.LogAsync(ex,"Teacher/Index");
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                _notyf.Error("دخول غي مصرح");
                return RedirectToAction("Login", "Account");
        }

        // GET: Teacher/Details/5
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "Teacher/Details");
                return NotFound();
            }

            ViewBag.Id = id ?? Guid.Empty;
            return View();
        }

        // GET: Teacher/Create
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public IActionResult Create()
        {
            return View();
        }

        // GET: Teacher/Edit/5
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet]        
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Teacher/Edit");
                return NotFound();
            }

            ViewBag.Id = id ?? Guid.Empty;
            return View();
        }

        public async Task<IActionResult> ManagerTeacher([FromQuery]Guid teacherId)
        {
            try{
                var students = await _context.StudentLectuerTeachers
                .Where(ts => ts.IdTeacher ==teacherId )
                .Include(ts => ts.IdStudentNavigation)
                .Include(ts => ts.IdTeacherNavigation)
                .Select(ts => new TeacherStudentsViewModel{
                    TeacherName = ts.IdTeacherNavigation.Name,
                    StudentId=ts.IdTeacherNavigation.Id,
                    StudentName = ts.IdStudentNavigation.Name,
                    ClassroomName = ts.IdClassNavigation.Name,
                    LectureName = ts.IdStudentNavigation.StudentLectuerTeachers.Select(sl => sl.IdLectuerNavigation.Name)
                    .FirstOrDefault()
                    })
                    .ToListAsync();
                
                
                return View(students);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Students(Guid? teacherId)
        {
            Guid Id;

            try
            {
                Id = teacherId ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/DownloadTeacherCertificate");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }
            
            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, Id, "Attendance/ViewAttendance");
            if (!IsValid)
            {
                
                return View(nameof(Index));
            }
            var name = _context.Teachers.FirstOrDefault(c => c.Id == IdTeacher);
            ViewBag.name = name?.Name??"Null";
            Console.WriteLine($"std Teacher Id: {IdTeacher}");
            ViewBag.IdTeacher = teacherId;
            return View();
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet]
        public async Task<IActionResult> ManagementClassLectuerForTeachers(Guid? idTeacher)
        {
            if (idTeacher == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Teacher/ManagementClassLectuerForTeachers");
                return NotFound();
            }

            ViewBag.Id = idTeacher;
            return View();
        }

        [NonAction]
        public async Task<IActionResult> GetStudentCountPerGrades(Guid? idTeacher)
        {
            Guid Id;

            try
            {
                Id = idTeacher ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }

            Console.WriteLine($"Id Teacher: {idTeacher}");
            var schoolId = HttpContext.Session.GetGuid("School");
            Console.WriteLine($"Id School: {schoolId}");
            var data = _context.Grades
                .Where(g =>
                    g.IdSchool == schoolId
                    && g.IdTeacher == Id
                    && g.IdStudentNavigation != null
                    && g.IdStudentNavigation.IsDeletedStudent == false
                    && g.IdLectuerNavigation != null
                    ).Include(l => l.IdClassNavigation)
                .GroupBy(g => new { g.IdLectuer, g.IdLectuerNavigation.Name })
                .Select(g => new
                {
                    LectuerName = g.Key.Name,
                    TotalStudents = g.Count(),
                    Below50 = g.Count(x => x.Total < 50) * 100.0 / g.Count(),
                    Below60 = g.Count(x => x.Total >= 50 && x.Total < 60) * 100.0 / g.Count(),
                    Below70 = g.Count(x => x.Total >= 60 && x.Total < 70) * 100.0 / g.Count(),
                    Below80 = g.Count(x => x.Total >= 70 && x.Total < 80) * 100.0 / g.Count(),
                    Below90 = g.Count(x => x.Total >= 80 && x.Total < 90) * 100.0 / g.Count(),
                    Below100 = g.Count(x => x.Total >= 90 && x.Total < 100) * 100.0 / g.Count(),
                    Equal100 = g.Count(x => x.Total == 100) * 100.0 / g.Count()
                })
                .ToList();
            if (!data.Any())
            {
                return Json(new { error = "No data available" });
            }


            return Json(data);
        }

        [NonAction]
        public async Task<IActionResult> GetStudentCountPerAttendance(Guid? idTeacher)
        {
            Guid Id;

            try
            {
                Id = idTeacher ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }

            try
            {
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, Id, "Teacher/GetStudentCountPerAttendance");
                if (!isValid)
                    return Forbid();

                var attendanceStats = await _context.Attendances
                    .AsNoTracking()
                    .Where(a => a.IdSchool == schoolId && a.IdTeacher == teacherId &&
                        a.IdLectuer.HasValue && !a.IsDeletedAttendance && !a.IsDeletedStudent &&
                        !a.IsDeletedLectuer && !a.IsDeletedTeacher && !a.IsDeletedSchool &&
                        !a.IsTeacherRemovedFromLectuer)
                    .GroupBy(a => a.IdLectuer!.Value)
                    .Select(group => new
                    {
                        LectuerId = group.Key,
                        TotalSessions = group.Count(),
                        AttendanceCount = group.Count(a => a.AttendanceStatus == "1"),
                        AbsenceCount = group.Count(a => a.AttendanceStatus == "0"),
                        ExcusedAbsenceCount = group.Count(a => a.AttendanceStatus == "m")
                    })
                    .ToListAsync();

                var lectureIds = attendanceStats.Select(item => item.LectuerId).ToList();
                var lectureNames = await _context.Lectuers
                    .AsNoTracking()
                    .Where(lecture => lecture.IdSchool == schoolId && lectureIds.Contains(lecture.Id))
                    .Select(lecture => new { lecture.Id, lecture.Name })
                    .ToDictionaryAsync(lecture => lecture.Id, lecture => lecture.Name);

                var data = attendanceStats.Select(item => new
                {
                    LectuerName = lectureNames.GetValueOrDefault(item.LectuerId, "Unknown"),
                    item.TotalSessions,
                    AttendancePercentage = item.TotalSessions == 0
                        ? 0
                        : item.AttendanceCount * 100.0 / item.TotalSessions,
                    AbsencePercentage = item.TotalSessions == 0
                        ? 0
                        : item.AbsenceCount * 100.0 / item.TotalSessions,
                    ExcusedAbsencePercentage = item.TotalSessions == 0
                        ? 0
                        : item.ExcusedAbsenceCount * 100.0 / item.TotalSessions
                });

                return Json(data);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/GetStudentCountPerAttendance");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Unable to load attendance statistics." });
            }
        }

        // شهادة قيد لطالب
        [AuthorizeRoles(RoleNames.Teacher, RoleNames.Admin, RoleNames.Manager)]
        [HttpGet]
        public async Task<IActionResult> DownloadTeacherCertificate(Guid? idTeacher)
        {
            var id = idTeacher ?? Guid.Empty;
            if (id == Guid.Empty)
                return BadRequest();

            try
            {
                Guid? allowedSchoolId = null;
                if (User.IsInRole(RoleNames.Teacher))
                {
                    var (isValid, _, schoolId, _) = await _sessionValidatorService
                        .ValidateTeacherSessionAsync(HttpContext, id, "Teacher/DownloadTeacherCertificate");
                    if (!isValid) return Forbid();
                    allowedSchoolId = schoolId;
                }
                else if (User.IsInRole(RoleNames.Manager))
                {
                    var (isValid, schoolId, _) = await _sessionValidatorService
                        .ValidateManagerSessionAsync(HttpContext, "Teacher/DownloadTeacherCertificate");
                    if (!isValid) return Forbid();
                    allowedSchoolId = schoolId;
                }

                var teacherQuery = _context.Teachers.AsNoTracking()
                    .Where(t => t.Id == id && !t.IsDeleted && !t.IsDeletedSchool)
                    .Include(t => t.IdSchoolNavigation)
                    .AsQueryable();
                if (allowedSchoolId.HasValue)
                    teacherQuery = teacherQuery.Where(t => t.IdSchool == allowedSchoolId.Value);

                var teacher = await teacherQuery.SingleOrDefaultAsync();
                if (teacher == null)
                {
                    return NotFound();
                }

                var managerName = await _context.Menegars.AsNoTracking()
                    .Where(m => m.IdSchool == teacher.IdSchool && !m.IsDeleted && !m.IsDeletedSchool)
                    .OrderBy(m => m.Id)
                    .Select(m => m.Name)
                    .FirstOrDefaultAsync() ?? "لم يتم اعتماده بعد.";

                var subjects = await _context.TeacherLectuerClasses.AsNoTracking()
                    .Where(tl => tl.IdTeacher == id && tl.IdSchool == teacher.IdSchool &&
                        !tl.IsDeletedTeacherLectuerClass && !tl.IsDeletedTeacher &&
                        !tl.IsDeletedLectuer && !tl.IsDeletedSchool &&
                        !tl.IsTeacherRemovedFromLectuer && tl.IdLectuerNavigation != null)
                    .Select(tl => tl.IdLectuerNavigation!.Name)
                    .Where(name => name != null && name != string.Empty)
                    .Distinct()
                    .ToListAsync();
                if (subjects.Count == 0)
                    subjects.Add("غير محددة");

                var document = new TeacherEnrollmentCertificate(
                    teacher.Name ?? "غير معرف",
                    teacher.IdNumber ?? 0,
                    teacher.IdSchoolNavigation?.Name ?? "غير معرف",
                    managerName,
                    subjects!);
                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                return File(stream, "application/pdf", $"شهادة_قيد_{teacher.Name ?? "غير معرف"}.pdf");

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/DownloadTeacherCertificate");
                _notyf.Error("حدث خطا اثناء انشاء شهادة قيد.\nيرجى المحاولة لاحقا");
                return RedirectToAction(nameof(Index));
            }
        }
        
        private bool TeacherExists(Guid id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}
