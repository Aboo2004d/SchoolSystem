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
        private readonly EncryptionHelper _encryptionHelper;


        public TeacherController(SystemSchoolDbContext context, EncryptionHelper encryptionHelper, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
            _encryptionHelper = encryptionHelper;
        }

        // GET: Teacher
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Index()
        {
            int IdSchool = HttpContext.Session.GetInt32("School")??0;
            if(IdSchool != 0){
                string Role = HttpContext.Session.GetString("Role")??"null";
                if (Role == "Teacher")
                {
                    int Id = HttpContext.Session.GetInt32("Id")??0;
                    Console.WriteLine($"Id: {Id}");
                    if (Id != 0)
                    {
                        Teacher? teacher = await _context.Teachers.Where(t => t.Id == Id && t.IdSchool == IdSchool).FirstOrDefaultAsync();
                        if (teacher != null)
                        {
                            Console.WriteLine($"Id Teacher123: {teacher.Id}");
                            TeacherViewModel teacherViewModel = new TeacherViewModel
                            {
                                Id = _encryptionHelper.EncryptInt(teacher.Id)
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
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Details(string? id)
        {
            if (id == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "Teacher/Details");
                return NotFound();
            }

            ViewBag.Id = id;
            return View();
        }

        // GET: Teacher/Create
        [AuthorizeRoles("admin")]
        public IActionResult Create()
        {
            return View();
        }

        // GET: Teacher/Edit/5
        [AuthorizeRoles("admin")]
        [HttpGet]        
        public async Task<IActionResult> Edit(string? id)
        {
            if (id == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Teacher/Edit");
                return NotFound();
            }

            ViewBag.Id = id;
            return View();
        }

        public async Task<IActionResult> ManagerTeacher([FromQuery]int teacherId)
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
        public async Task<IActionResult> Students(string? teacherId)
        {
            int Id;

            try
            {
                Id = _encryptionHelper.DecryptInt(teacherId??"0");

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

        [AuthorizeRoles("admin")]
        [HttpGet]
        public async Task<IActionResult> ManagementClassLectuerForTeachers(string? idTeacher)
        {
            if (idTeacher == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Teacher/ManagementClassLectuerForTeachers");
                return NotFound();
            }

            ViewBag.Id = idTeacher;
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> GetStudentCountPerGrades(string? idTeacher)
        {
            int Id;

            try
            {
                Id = _encryptionHelper.DecryptInt(idTeacher??"0");

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }

            Console.WriteLine($"Id Teacher: {idTeacher}");
            var schoolId = HttpContext.Session.GetInt32("School");
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

        [HttpGet]
        public async Task<IActionResult> GetStudentCountPerAttendance(string? idTeacher)
        {
            int Id;

            try
            {
                Id = _encryptionHelper.DecryptInt(idTeacher??"0");

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }

            var schoolId = HttpContext.Session.GetInt32("School");

            var data = _context.Attendances
                .Where(g => g.IdSchool == schoolId
                            && g.IdTeacher == Id
                            && g.IdStudentNavigation != null
                            && g.IdStudentNavigation.IsDeletedStudent == false
                            && g.IdLectuerNavigation != null)
                .GroupBy(g => new { g.IdLectuer, g.IdLectuerNavigation.Name })
                .Select(g => new
                {
                    LectuerName = g.Key.Name,
                    TotalSessions = g.Count(),
                    AttendancePercentage = g.Count(x => x.AttendanceStatus == "1") * 100.0 / g.Count(),
                    AbsencePercentage = g.Count(x => x.AttendanceStatus == "0") * 100.0 / g.Count(),
                    ExcusedAbsencePercentage = g.Count(x => x.AttendanceStatus == "m") * 100.0 / g.Count()
                })
                .ToList();

            return Json(data);
        }

        // شهادة قيد لطالب
        [AuthorizeRoles("Teacher","admin")]
        public async Task<IActionResult> DownloadTeacherCertificate(string? idTeacher)
        {
            int Id;

            try
            {
                Id = _encryptionHelper.DecryptInt(idTeacher??"0");

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/DownloadTeacherCertificate");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }
            try
            {
                Teacher? teacher = _context.Teachers
                .Where(s => s.Id == Id && s.IsDeleted == false && s.IdSchool == HttpContext.Session.GetInt32("School"))
                .Include(s => s.IdSchoolNavigation).SingleOrDefault();
                if (teacher == null)
                {
                    await _logger.LogAsync(new Exception("انتهت صلاحية الجلسة"), "Teacher/DownloadTeacherCertificate");
                    _notyf.Error("انتهت الجلسة.");
                    return RedirectToAction("Logout", "Account");
                }
                Menegar? menegar = _context.Menegars.SingleOrDefault(m => m.IdSchool == teacher.IdSchool);

                var document = new TeacherEnrollmentCertificate(
                    teacher?.Name ?? "غير معرف",
                    teacher?.IdNumber ?? 0,
                    teacher?.IdSchoolNavigation?.Name ?? "غير معرف",
                    menegar?.Name ?? "لم يتم اعتماده بعد.",
                    _context.TeacherLectuerClasses.Where(tl => tl.IdTeacher == Id && teacher.IdSchool == teacher.IdSchool).Select(name => name.IdLectuerNavigation.Name).ToList());
                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                return File(stream, "application/pdf", $"شهادة_قيد_{teacher?.Name ?? "غير معرف"}.pdf");

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Student/DownloadStudentCertificate");
                _notyf.Error("حدث خطا اثناء انشاء شهادة قيد.\nيرجى المحاولة لاحقا");
                return View(nameof(Index));
            }
        }
        
        private bool TeacherExists(int id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}
