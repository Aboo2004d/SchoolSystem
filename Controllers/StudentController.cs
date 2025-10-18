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
using NuGet.Protocol;
using QuestPDF.Fluent;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;
        private readonly EncryptionHelper _encryptionHelper;


        public StudentController(SystemSchoolDbContext context, EncryptionHelper encryptionHelper, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
            _encryptionHelper = encryptionHelper;
            
        }

        // GET: Student
        [AuthorizeRoles("Student")]
        public async Task<IActionResult> Index()
        {
            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdStudent, IdSchool,status) = await _sessionValidatorService
            .ValidateStudentSessionAsync(HttpContext, HttpContext.Session.GetInt32("Id")??-1, "Attendance/DataAttendance");
            if (!IsValid)
            {
                _notyf.Error("انتهت الجلسة");
                return RedirectToAction("Logout", "Account");
            }

            Student? student = await _context.Students.SingleOrDefaultAsync(s => s.Id == IdStudent && s.IdSchool == IdSchool);

            return View(student);
        }

        // GET: Student/Details/5
        [AuthorizeRoles("admin")]
        public IActionResult Details(string? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewBag.Id = id;

            return View();
        }

        // GET: Student/Create
        [AuthorizeRoles("admin")]
        public IActionResult Create()
        {
            ViewBag.Class = new SelectList(_context.TheClasses.Where(c => c.IdSchool == HttpContext.Session.GetInt32("School")), "Id", "Name");
            return View();
        }

        // GET: Student/Edit/5
        [AuthorizeRoles("admin")]
        public async Task<ActionResult> Edit(string? id)
        {
            if (id == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات الطالب المرسلة"), "Student/Details");
                return NotFound();
            }
            ViewBag.Id = id;
            return View();
            
        }
        
        [HttpGet]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ChangeClass(string? idStudent)
        {
            if (idStudent == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "StudentApi/ChangeClass");
                return NotFound();
            }
            ViewBag.IdStudent = idStudent;

            return View();
        }

        /*[HttpPost]
        [AuthorizeRoles("admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeClass(ManagerMenegarStudentInClassViewModel student)
        {
            if (student.IdClass == null || student.IdStudent == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                return RedirectToAction("ManagerMenegarClassView", "Menegar");
            }

            int IdStudent;
            try
            {
                IdStudent = _encryptionHelper.DecryptInt(student.IdStudent);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Student/ChangeClass");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarClassView", "Menegar");
            }
            try
            {
                Student? std = await _context.Students.FindAsync(IdStudent);
                TheClass? classes = await _context.TheClasses.FindAsync(student.IdClass);
                if (std == null || classes == null)
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    return RedirectToAction("ManagerMenegarClassView", "Menegar");
                }
                std.IdClass = student.IdClass;

                TeacherLectuerClass? teacherLectuerClass = await _context.TeacherLectuerClasses
                    .Where(sclt => sclt.IdClass == std.IdClass && sclt.IdSchool == std.IdSchool)
                    .FirstOrDefaultAsync();

                List<Grade>? grade = await _context.Grades
                    .Where(g => g.IdStudent == std.Id)
                    .ToListAsync();
                foreach (var item in grade)
                {
                    item.IdClass = student.IdClass;
                    item.IdTeacher = teacherLectuerClass?.IdTeacher;
                }
                List<Attendance>? attendances = await _context.Attendances
                    .Where(g => g.IdStudent == std.Id)
                    .ToListAsync();
                foreach (var item in attendances)
                {
                    item.IdTeacher = teacherLectuerClass?.IdTeacher;
                    item.IdClass = student.IdClass;
                }

                List<StudentLectuerTeacher>? studentLectuerTeachers = await _context.StudentLectuerTeachers
                    .Where(g => g.IdStudent == std.Id)
                    .ToListAsync();
                foreach (var item in studentLectuerTeachers)
                {
                    item.IdClass = student.IdClass;
                    item.IdTeacher = teacherLectuerClass?.IdTeacher;
                }

                await _context.SaveChangesAsync();
                _notyf.Success("تمت عملية التحديث بنجاح");
                return RedirectToAction("ManagerMenegarStudentInClassView", "Menegar", new { idClass = student.LastIdClass ?? "0" });


            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Student/ChangeClass");
                _notyf.Error("حدث خطأ غير متوقع يرجى المحاولة لاحقا.");
                return RedirectToAction("ManagerMenegarStudentInClassView", "Menegar", new { idClass = student.LastIdClass ?? "0" });
            }

        }
       */ // GET: Student/Delete/5
        /*[AuthorizeRoles("admin")]
        public async Task<IActionResult> Delete(string? id)
        {
            if (id == null)
            {
                await _logger.LogAsync(new Exception("المرسل فارغ"), "Student/Delete");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                return RedirectToAction("ManagerMenegarStudentView","Menegar");
            }

            int Id;

            try
            {
                Id = _encryptionHelper.DecryptInt(id);

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Student/Delete");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarClassView","Menegar");
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == Id);
            if (student == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Student/Delete");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                return RedirectToAction("ManagerMenegarStudentView","Menegar");
            }

            return View(student);
        }
*/
        // POST: Student/Delete/5
        
        
        [AuthorizeRoles("Student")]
        public JsonResult GetStudentCountPerGrades(int? idStudent)
        {
            var schoolId = HttpContext.Session.GetInt32("School");

            var data = _context.Grades
                .Where(g => g.IdSchool == schoolId
                            && g.IdStudent == idStudent
                            && g.IdStudentNavigation != null
                            && g.IdStudentNavigation.IsDeletedStudent == false 
                            && g.IdLectuerNavigation != null)
                .Select(g => new
                {
                    LectuerName = g.IdLectuerNavigation.Name,
                    TotalSessions = g.Total
                })
                .ToList();

            return Json(data);
        }

        [AuthorizeRoles("Student")]
        public JsonResult GetStudentCountPerAttendance(int? idStudent)
        {
            var schoolId = HttpContext.Session.GetInt32("School");

            var studentAttendances = _context.Attendances
                .Where(g => g.IdSchool == schoolId
                            && g.IdStudent == idStudent
                            && g.IdStudentNavigation != null
                            && g.IdStudentNavigation.IsDeletedStudent == false
                            && g.IdLectuerNavigation != null).Include(l => l.IdLectuerNavigation)
                .ToList();
            Console.WriteLine($"Student Attendances Count: {studentAttendances.Count}");
            Console.WriteLine($"Student Attendances Lectuer: {studentAttendances[0].IdLectuerNavigation?.Name??"Null"}");

            var result = studentAttendances
                .GroupBy(a => new { a.IdLectuer, a.IdLectuerNavigation.Name }) // التجميع حسب اسم المادة
                .Select(g =>
                {
                    int totalSessions = g.Count();
                    int presentCount = g.Count(x => x.AttendanceStatus == "1");
                    int excusedCount = g.Count(x => x.AttendanceStatus == "m");

                    double presentPercentage = totalSessions > 0 ? (presentCount * 100.0) / totalSessions : 0;
                    double excusedPercentage = totalSessions > 0 ? (excusedCount * 100.0) / totalSessions : 0;

                    return new
                    {
                        SubjectName = g.Key.Name,
                        TotalSessions = totalSessions,
                        PresentCount = presentCount,
                        ExcusedCount = excusedCount,
                        PresentPercentage = Math.Round(presentPercentage, 2),
                        ExcusedPercentage = Math.Round(excusedPercentage, 2)
                    };
                })
                .ToList();

            return Json(result);
        }

        // شهادة قيد لطالب
        [AuthorizeRoles("admin","Student")]
        public IActionResult DownloadStudentCertificate(int? idStudent)
        {
            try
            {
                Student? student = _context.Students
                .Where(s => s.Id == idStudent && s.IsDeletedStudent == false && s.IdSchool == HttpContext.Session.GetInt32("School"))
                .Include(s => s.IdClassNavigation).Include(s => s.IdSchoolNavigation).SingleOrDefault();
                if (student == null)
                {
                    _logger.LogAsync(new Exception("انتهت صلاحية الجلسة"), "Student/DownloadStudentCertificate");
                    _notyf.Error("انتهت الجلسة.");
                    return RedirectToAction("Logout", "Account");
                }
                Menegar? menegar = _context.Menegars.SingleOrDefault(m => m.IdSchool == student.IdSchool);

                var document = new StudentEnrollmentCertificate(
                    student?.Name??"غير معرف", student?.IdNumber??0,
                    student?.IdClassNavigation?.Name??"غير معرف",
                    student?.IdSchoolNavigation?.Name??"غير معرف",
                    menegar?.Name??"لم يتم اعتماده بعد.");
                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                return File(stream, "application/pdf", $"شهادة_قيد_{student?.Name??"غير معرف"}.pdf");

            }
            catch (Exception ex)
            {
                _logger.LogAsync(ex, "Student/DownloadStudentCertificate");
                _notyf.Error("حدث خطا اثناء انشاء شهادة قيد.\nيرجى المحاولة لاحقا");
                if (HttpContext.Session.GetString("Role") == "Student")
                    return View(nameof(Index));
                return View(nameof(Details));
            }
        }

        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
