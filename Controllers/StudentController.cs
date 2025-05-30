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
        

        public StudentController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
        }

        // GET: Student
        [AuthorizeRoles("Student")]
        public async Task<IActionResult> Index()
        {
            string Role = HttpContext.Session.GetString("Role") ?? "null";
            if(Role == "Student"){
                int StudentId = HttpContext.Session.GetInt32("Id")??0;
                if(StudentId == 0){
                    _notyf.Error("Student Not Found");
                    return RedirectToAction("Index","Home");
                }
                var avg = _context.Grades
                .Where(g => g.IdStudent == StudentId && g.Total != null)
                .Select(g => g.Total.Value)
                .ToList()  // تحويل النتيجة لقائمة في الذاكرة
                .DefaultIfEmpty(0)
                .Average(); // حساب المتوسط
                ViewBag.Avg = avg;
                return View(await _context.Students.Where(s => s.Id == StudentId).FirstOrDefaultAsync());

            }
            _notyf.Error("Unauthorized entry!");
            return RedirectToAction("Index","Home");
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .Include(c => c.IdClassNavigation)
                .Include(sch => sch.IdSchoolNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound("Student not found");
            }

            return View(student);
        }

        // GET: Student/Create
        public IActionResult Create()
        {
            ViewBag.Class = new SelectList(_context.TheClasses.Where(c => c.IdSchool == HttpContext.Session.GetInt32("School")), "Id", "Name");
            return View();
        }

        // POST: Student/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Phone,Email,TheDate,IdClass,IdNumber,City,Area")] Student student)
        {
            Exception ex = new Exception();
            if (ModelState.IsValid)
            {
                if (student.Name != null && student.Phone != null && student.Email != null
                && student.TheDate != null && student.IdClass != null && student.IdNumber != null &&student.City != null && student.Area != null)
                {
                    string Role = HttpContext.Session.GetString("Role") ?? "Null";
                    var school = HttpContext.Session.GetInt32("School") ?? 0;
                    if (school != 0)
                    {
                        if (Role == "admin")
                        {
                            if (_context.Students.Any(s => s.IdNumber == student.IdNumber))
                            {
                                _notyf.Error("لا يمكن تكرار رقم الهوية");
                                ViewBag.Class = new SelectList(_context.TheClasses, "Id", "Name");
                                return View(student);
                            }
                            if (student.TheDate >= new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day))
                            {
                                _notyf.Error("تاريخ الميلاد غير صالح");
                                ViewBag.Class = new SelectList(_context.TheClasses, "Id", "Name");
                                return View(student);
                            }
                            student.IdSchool = school;
                            _context.Students.Add(student);
                            await _context.SaveChangesAsync();
                            Student? std = await _context.Students.Where(sclt => sclt.IdNumber == student.IdNumber).FirstOrDefaultAsync();
                            if (std == null)
                            {
                                _notyf.Error("فشلت عملية الحفظ");
                                ex = new Exception("Save failed");
                                await _logger.LogAsync(ex, "Student/Create");
                                ViewBag.Class = new SelectList(_context.TheClasses, "Id", "Name");
                                return View(student);
                            }
                            var studentClass = await _context.TeacherLectuerClasses.Where(sclt => sclt.IdClass == std.IdClass)
                                .ToListAsync();
                            if (studentClass != null)
                            {
                                foreach (var item in studentClass)
                                {
                                    var studentLectuer = new StudentLectuerTeacher
                                    {
                                        IdStudent = std.Id,
                                        IdClass = std.IdClass,
                                        IdTeacher = item.IdTeacher,
                                        IdLectuer = item.IdLectuer,
                                        IdSchool = std.IdSchool
                                        
                                    };
                                    _context.StudentLectuerTeachers.Add(studentLectuer);

                                }
                                await _context.SaveChangesAsync();
                            }

                            _notyf.Success("تمت عملية الاضافة بنجاح");
                            return RedirectToAction("ManagerMenegarStudentView", "Menegar");
                        }
                        ex = new Exception("Bypass verification system");
                        await _logger.LogAsync(ex, "Student/Create");
                        _notyf.Error("دخول غير مصرح به");
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("Login", "Account");
                    }
                }
            }
            _notyf.Error("يرجى تعبئة جميع الحقول بالبيانات الصحيحة");
            ViewBag.Class = new SelectList(_context.TheClasses, "Id", "Name");
            return View(student);
        }
        
        // GET: Student/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            Exception e = new Exception();
            if (id == null)
            {
                _notyf.Error($"لا يمكن التلاعب بالبيانات الطالب المرسلة");
                e = new Exception("التلاعب بالبيانات الطالب المرسلة");
                await _logger.LogAsync(e, "Student/Details");
                return RedirectToAction("Details", "Student");
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                _notyf.Error($"لا يمكن التلاعب ببيانات الطالب المرسلة");
                e = new Exception("التلاعب ببيانات الطالب المرسلة");
                await _logger.LogAsync(e, "Student/Edit");
                return RedirectToAction("Details", "Student");
            }
            ViewBag.Class = new SelectList(_context.TheClasses.Where(c => c.IdSchool == HttpContext.Session.GetInt32("School")), "Id", "Name");
            return View(student);
        }

        // POST: Student/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Phone,Email,TheDate,IdClass,City,Area")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                Exception ex = new Exception();
                try
                {
                    string Role = HttpContext.Session.GetString("Role") ?? "Null";
                    int school = HttpContext.Session.GetInt32("School") ?? 0;
                    if (school != 0)
                    {
                        if (Role == "admin")
                        {
                            Student? std = await _context.Students.FindAsync(id);
                            if (std == null)
                            {
                                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                                ex = new Exception("التلاعب بالبيانات المرسلة");
                                await _logger.LogAsync(ex, "Student/Edit");
                                ViewBag.Class = new SelectList(_context.TheClasses.Where(c => c.IdSchool == school), "Id", "Name");
                                return View(student);
                            }

                            if (student.TheDate >= new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day))
                                {
                                    _notyf.Error("تاريخ الميلاد غير صالح");
                                    ViewBag.Class = new SelectList(_context.TheClasses, "Id", "Name");
                                    return View(student);
                                }
                            std.Name = student.Name;
                            std.Phone = student.Phone;
                            std.Email = student.Email;
                            std.TheDate = student.TheDate;
                            std.City = student.City;
                            std.Area = student.Area;
                            std.IdClass = student.IdClass;
                            std.IdSchool = school;
                            await _context.SaveChangesAsync();


                            _notyf.Success("تمت عملية الاضافة بنجاح");
                            return RedirectToAction("ManagerMenegarStudentView", "Menegar");
                        }
                        ex = new Exception("Bypass verification system");
                        await _logger.LogAsync(ex, "Student/Create");
                        _notyf.Error("دخول غير مصرح به");
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        return RedirectToAction("Login", "Account");
                    }
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(student);
        }

        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                student.IsDeleted = true;
                _notyf.Success("تم حذف الطالب بنجاح");
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("ManagerMenegarStudentView","Menegar");
        }
        
        public async Task<IActionResult> ManagerStudentLectuer([FromQuery]int idstudent)
        {
            try{
                Console.WriteLine($"idStudent: {idstudent}");
                var students = await _context.StudentLectuerTeachers
                .Where(ts => ts.IdStudent ==idstudent )
                .Include(ts => ts.IdStudentNavigation)
                .Include(sl => sl.IdLectuerNavigation)
                .Select(ts => new StudentLectuerViewModel{
                    Id= ts.Id,
                    TeacherName = ts.IdTeacherNavigation.Name??"Null",
                    StudentName = ts.IdStudentNavigation.Name??"Null",
                    IdStudent = ts.IdStudentNavigation.Id,
                    ClassroomName = ts.IdClassNavigation.Name??"Null",
                    LectureName = ts.IdLectuerNavigation.Name??"Null"
                    })
                    .ToListAsync(); 
                return View(students);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> StudentLectuerReturn([FromQuery]int idstudent)
        {
            try{
                Console.WriteLine($"idStudent: {idstudent}");
                var students = await _context.StudentLectuerTeachers
                .Where(ts => ts.IdStudent ==idstudent )
                .Include(ts => ts.IdStudentNavigation)
                .Include(sl => sl.IdLectuerNavigation)
                .Select(ts => new StudentLectuerReturnViewModel{
                    Id= ts.Id,
                    TeacherName = ts.IdTeacherNavigation.Name??"Null",
                    StudentName = ts.IdStudentNavigation.Name??"Null",
                    IdStudent = ts.IdStudentNavigation.Id,
                    ClassroomName = ts.IdClassNavigation.Name??"Null",
                    LectureName = ts.IdLectuerNavigation.Name??"Null"
                    })
                    .ToListAsync(); 
                return View(students);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
        
        public JsonResult GetStudentCountPerGrades(int? idStudent)
        {
            var schoolId = HttpContext.Session.GetInt32("School");

            var data = _context.Grades
                .Where(g => g.IdSchool == schoolId
                            && g.IdStudent == idStudent
                            && g.IdStudentNavigation != null
                            && (g.IdStudentNavigation.IsDeleted == false || g.IdStudentNavigation.IsDeleted == null)
                            && g.IdLectuerNavigation != null)
                .Select(g => new
                {
                    LectuerName = g.IdLectuerNavigation.Name,
                    TotalSessions = g.Total
                })
                .ToList();

            return Json(data);
        }

        public JsonResult GetStudentCountPerAttendance(int? idStudent)
        {
            var schoolId = HttpContext.Session.GetInt32("School");

            var studentAttendances = _context.Attendances
                .Where(g => g.IdSchool == schoolId
                            && g.IdStudent == idStudent
                            && g.IdStudentNavigation != null
                            && (g.IdStudentNavigation.IsDeleted == false || g.IdStudentNavigation.IsDeleted == null)
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


        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
