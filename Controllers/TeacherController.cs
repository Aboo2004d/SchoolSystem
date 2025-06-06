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
                            return View(teacher);
                        }
                    }
                }

            }
                // إذا كان المستخدم مصادقًا عليه بالفعل، قم بإعادة توجيهه إلى الصفحة الرئيسية
                Exception ex = new Exception("Bypass verification system");
                await _logger.LogAsync(ex,"Teacher/Index");
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _notyf.Error("دخول غي مصرح");
                return RedirectToAction("Login", "Account");
        }

        // GET: Teacher/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // GET: Teacher/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Teacher/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Phone,Email,TheDate,IdNumber,City,Area")]Teacher teacher)
        {
            if (ModelState.IsValid)
            {
                Teacher teacher1 = await _context.Teachers.FirstOrDefaultAsync(t => t.IdNumber == teacher.IdNumber);
                if (teacher1 != null)
                {
                    _notyf.Error("رقم الهوية موجود مسبقا");
                    return View(teacher);
                }
                if (teacher.TheDate > new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day))
                {
                    _notyf.Error("تاريخ الميلاد غير صالح");
                    return View(teacher);
                }
                teacher.IdSchool = HttpContext.Session.GetInt32("School") ?? 0;
                _context.Add(teacher);
                await _context.SaveChangesAsync();
                _notyf.Success("تم إضافة المعلم بنجاح");
                return RedirectToAction("ManagerMenegarTeacherView", "Menegar");
            }
            return View(teacher);
        }

        // GET: Teacher/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }
            return View(teacher);
        }

        // POST: Teacher/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Phone,Email,TheDate,IdNumber,City,Area")] Teacher teacher)
        {
            if (id != teacher.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teacher);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherExists(teacher.Id))
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
            return View(teacher);
        }

        // GET: Teacher/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacher = await _context.Teachers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            return View(teacher);
        }

        // POST: Teacher/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher != null)
            {
                teacher.IsDeleted = true;
                _notyf.Success("تم حذف المعلم بنجاح");;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManagerMenegarTeacherView", "Menegar");
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
        public async Task<IActionResult> ManagerStudentToTeacher(
            int teacherId,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                Console.WriteLine($"Id Teach: {teacherId}");
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, teacherId, "Attendance/DataAttendance");
                if (!IsValid)
                {
                    return Json(new { success = false, status = status, error = "Unauthorized access. Session expired." });
                }

                //فحص اذا كان تم ارسال قيمة المتغير ام لا و وصع قيمة افتراضية اذا كان لا
                if (length <= 0)
                    length = 10;

                // تحديد قيمة الـ searchValue
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.StudentLectuerTeachers.Where(std => std.IdSchool == IdSchool && std.IdTeacher == IdTeacher)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.StudentLectuerTeachers.Where(std => std.IdSchool == IdSchool && std.IdTeacher == IdTeacher)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "Unknown",
                        IdStudent = s.IdStudent,
                        StudentName = s.IdStudentNavigation != null ? s.IdStudentNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        IdClass = s.IdClass
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.StudentName != null && s.StudentName.Contains(searchValue))||
                        (s.LectuerName != null && s.LectuerName.Contains(searchValue))||
                        (s.ClassroomName != null && s.ClassroomName.Contains(searchValue))
                    );
                }

                // عدد السجلات الاصلية التي تنطبق عليها الشروط
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

                //ارسال بيانات للعرض
                var students = data.
                Select(s => new ManagerMenegarStudentInClassViewModel
                {
                    Id = s.Id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    IdClass = s.IdClass,
                    IdStudent = s.IdStudent,
                    LectuerName = s.LectuerName
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
                // حال كان هناك خطأ غير متوقع
                await _logger.LogAsync(e, "Attendance/DataAttendance");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Students(int teacherId)
        {
            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, teacherId, "Attendance/ViewAttendance");
            if (!IsValid)
            {
                if (!status)
                    return RedirectToAction("Login", "Account");
                return View(nameof(Index));
            }
            var name = _context.Teachers.FirstOrDefault(c => c.Id == IdTeacher);
            ViewBag.name = name?.Name??"Null";
            Console.WriteLine($"std Teacher Id: {IdTeacher}");
            ViewBag.IdTeacher = IdTeacher;
            return View();
        }

        [HttpGet]
        public JsonResult GetStudentCountPerGrades(int? idTeacher)
        {
            Console.WriteLine($"Id Teacher: {idTeacher}");
            var schoolId = HttpContext.Session.GetInt32("School");
            Console.WriteLine($"Id School: {schoolId}");
            var data = _context.Grades
                .Where(g =>
                    g.IdSchool == schoolId &&
                    g.IdTeacher == idTeacher
                    && g.IdStudentNavigation != null 
                    &&(g.IdStudentNavigation.IsDeleted == false || g.IdStudentNavigation.IsDeleted == null) &&
                    g.IdLectuerNavigation != null
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
        public JsonResult GetStudentCountPerAttendance(int? idTeacher)
        {
            var schoolId = HttpContext.Session.GetInt32("School");

            var data = _context.Attendances
                .Where(g => g.IdSchool == schoolId
                            && g.IdTeacher == idTeacher
                            && g.IdStudentNavigation != null
                            && (g.IdStudentNavigation.IsDeleted == false || g.IdStudentNavigation.IsDeleted == null)
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
        [AuthorizeRoles("Teacher")]
        public IActionResult DownloadTeacherCertificate(int? idTeacher)
        {
            Console.WriteLine($"Id Teacher: {idTeacher}");
            try
            {
                Teacher? teacher = _context.Teachers
                .Where(s => s.Id == idTeacher && s.IsDeleted == false && s.IdSchool == HttpContext.Session.GetInt32("School"))
                .Include(s => s.IdSchoolNavigation).SingleOrDefault();
                if (teacher == null)
                {
                    _logger.LogAsync(new Exception("انتهت صلاحية الجلسة"), "Teacher/DownloadTeacherCertificate");
                    _notyf.Error("انتهت الجلسة.");
                    return RedirectToAction("Logout", "Account");
                }
                Menegar? menegar = _context.Menegars.SingleOrDefault(m => m.IdSchool == teacher.IdSchool);

                var document = new TeacherEnrollmentCertificate(
                    teacher?.Name ?? "غير معرف",
                    teacher?.IdNumber ?? 0,
                    teacher?.IdSchoolNavigation?.Name ?? "غير معرف",
                    menegar?.Name ?? "لم يتم اعتماده بعد.",
                    _context.TeacherLectuerClasses.Where(tl => tl.IdTeacher == teacher.Id && teacher.IdSchool == teacher.IdSchool).Select(name => name.IdLectuerNavigation.Name).ToList());
                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                return File(stream, "application/pdf", $"شهادة_قيد_{teacher?.Name ?? "غير معرف"}.pdf");

            }
            catch (Exception ex)
            {
                _logger.LogAsync(ex, "Student/DownloadStudentCertificate");
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
