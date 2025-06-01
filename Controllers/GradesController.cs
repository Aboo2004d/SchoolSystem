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
    
    public class GradesController : Controller
    {
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly SystemSchoolDbContext _context;
        private readonly SessionValidatorService _sessionValidatorService;


        public GradesController(SystemSchoolDbContext context, INotyfService notyf, IErrorLoggerService logger, SessionValidatorService sessionValidatorService)
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
            int teacherId,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, teacherId, "Grades/DataGrades");
                if (!IsValid)
                {
                    return Json(new { success = false, status = status, error = "Unauthorized access. Session expired." });
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
                    IdClass = s.idClass,
                    IdStudent = s.idStudent,
                    LectuerName = s.LectuerName,
                    IdTeacher = s.idTeacher,
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
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> ViewGrades(int teacherId)
        {
            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, teacherId, "Grades/DataGrades");
            if (!IsValid)
            {
                if (!status)
                    return RedirectToAction("Login", "Account");
                return RedirectToAction("Index", "Teacher");
            }
            ViewBag.IdTeacher = Request.Query["IdTeacher"];
            return View();
        }

        // GET: Grades/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.IdLectuerNavigation)
                .Include(g => g.IdStudentNavigation)
                .Include(g => g.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.GradesId == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }
        [HttpGet]
        // GET: Grades/Create
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Create(int teacherId, int subjectId, int gradeId)
        {
            bool teacher = _context.Teachers.Any(t => t.Id ==teacherId);
            if(!teacher){
                Exception ex = new Exception();
                int Id = HttpContext.Session.GetInt32("Id")??0;
                    if(Id != 0){
                        _notyf.Error("The transmitted data cannot be tampered with.");
                        ex = new Exception("Manipulation of transmitted data");
                        await _logger.LogAsync(ex,"Grades/Create");
                        return RedirectToAction("Index",new{teacherId =Id });
                    }
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _notyf.Error("Unauthenticated user.");
                    ex = new Exception("Unauthenticated user.");
                    await _logger.LogAsync(ex, "Grades/Create");
                    return RedirectToAction("Index","Home");
            }
            bool Lectuer = _context.Lectuers.Any(t => t.Id ==subjectId);
            if(!Lectuer){
                Exception ex = new Exception();
                int Id = HttpContext.Session.GetInt32("Id")??0;
                    if(Id != 0){
                        _notyf.Error("The transmitted data cannot be tampered with.");
                        ex = new Exception("Manipulation of transmitted data");
                        await _logger.LogAsync(ex,"Grades/Create");
                        return RedirectToAction("Index",new{teacherId =Id });
                    }
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _notyf.Error("Unauthenticated user.");
                    ex = new Exception("Unauthenticated user.");
                    await _logger.LogAsync(ex, "Grades/Create");
                    return RedirectToAction("Index","Home");
            }
            bool grade = _context.Genders.Any(t => t.Id ==gradeId);
            if(!Lectuer){
                Exception ex = new Exception();
                int Id = HttpContext.Session.GetInt32("Id")??0;
                    if(Id != 0){
                        _notyf.Error("The transmitted data cannot be tampered with.");
                        ex = new Exception("Manipulation of transmitted data");
                        await _logger.LogAsync(ex,"Grades/Create");
                        return RedirectToAction("Index",new{teacherId =Id });
                    }
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _notyf.Error("Unauthenticated user.");
                    ex = new Exception("Unauthenticated user.");
                    await _logger.LogAsync(ex, "Grades/Create");
                    return RedirectToAction("Index","Home");
            }
            var students = _context.Students
                .Where(student =>
                _context.StudentLectuerTeachers.Any(stl => stl.IdClass == gradeId
                && stl.IdLectuer == subjectId && stl.IdTeacher == teacherId)
                && _context.TeacherLectuerClasses.Any(tlc => tlc.IdClass == gradeId && tlc.IdTeacher == teacherId)
                && student.IdClass == gradeId
                )
                .ToList();

            var studentIds = students.Select(s=>(int?)s.Id).ToList();

            var existingGrades = _context.Grades
                .Where(g => g.IdTeacher == teacherId && g.IdLectuer == subjectId && studentIds.Contains(g.IdStudent))
                .Include(g => g.IdStudentNavigation)
                .ToList();

            var studentsWithGrades = students.Select(student =>
            {
                var grade = existingGrades.FirstOrDefault(g => g.IdStudent == student.Id);

                // إذا لم تكن هناك درجة، أنشئ واحدة جديدة بالقيم صفرية
                if (grade == null)
                {
                    grade = new Grade
                    {
                        IdStudent = student.Id,
                        IdTeacher = teacherId,
                        IdLectuer = subjectId,
                        IdStudentNavigation = student
                    };
                }
                else
                {
                    // ضمان أن القيم الفارغة = 0
                    grade.FirstMonth ??= 0;
                    grade.Mid ??= 0;
                    grade.SecondMonth ??= 0;
                    grade.Activity ??= 0;
                    grade.Final ??= 0;
                    grade.Total ??= 0;
                }

                return new { Student = student, Grade = grade };
                }).ToList();
                string lectuer = _context.Lectuers.SingleOrDefault(l => l.Id == subjectId)?.Name??"غير معرف";
                string Classes = _context.TheClasses.SingleOrDefault(c => c.Id == gradeId)?.Name??"غير معرف";
                ViewBag.StudentsWithGrades = studentsWithGrades;
                ViewBag.SubjectId = subjectId;
                ViewBag.SubjectName = lectuer;
                ViewBag.GradeName = Classes;
                ViewBag.GradeId = gradeId;
                ViewBag.TeacherId = teacherId;

            return View();
        }

        
        [HttpPost]
        [AuthorizeRoles("Teacher")]
        public IActionResult SaveAll(List<GradeInputViewModel> Grades, int teacherId, int subjectId)
        {
            try
            {
                foreach (var item in Grades)
                {
                    Grade? grade = _context.Grades
                        .FirstOrDefault(g => g.IdStudent == item.StudentId && g.IdTeacher == teacherId && g.IdLectuer == subjectId);

                    var std = _context.Students.FirstOrDefault(s => s.Id == item.StudentId);
                    if (std == null)
                    {
                        return NotFound("Student Not Found"); // Skip if student not found
                    }
                    if (grade == null)
                    {
                        grade = new Grade
                        {
                            IdStudent = item.StudentId,
                            IdTeacher = teacherId,
                            IdLectuer = subjectId,
                            IdSchool = std.IdSchool,
                            IdClass = std.IdClass
                        };
                        _context.Grades.Add(grade);
                    }

                    grade.FirstMonth = item.FirstMonth;
                    grade.Mid = item.Mid;
                    grade.SecondMonth = item.SecondMonth;
                    grade.Activity = item.Activity;
                    grade.Final = item.Final;
                    grade.IdClass = std.IdClass;
                    grade.IdSchool = std.IdSchool;

                    // Total يُحسب تلقائيًا
                }
                _context.SaveChanges();
                _notyf.Success("تم اضافة العلامات للطلاب بنجاح");
                return RedirectToAction("ViewGrades", new { teacherId });

            }
            catch (Exception ex)
            {
                _notyf.Error("فشل تسجيل العلامات للطلاب\nحاول مرة اخرى لاحقا");
                _logger.LogAsync(ex, "Grades/SaveAll");
                return RedirectToAction("ViewGrades", new { teacherId });
            }
        }
        
        [HttpGet]
        [AuthorizeRoles("Student","admin")]
        public async Task<IActionResult> DataGradesStudent(
            int studentid,
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
                    IdClass = s.idClass,
                    IdStudent = s.idStudent,
                    LectuerName = s.LectuerName,
                    IdTeacher = s.idTeacher,
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


        [HttpGet]
        [AuthorizeRoles("Student","admin")]
        public async Task<IActionResult> MarkStudent(int studentid)
        {
            if (HttpContext.Session.GetString("Role") == "admin")
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdStudent, IdSchool, status) = await _sessionValidatorService.ValidateStudentSessionAsync(HttpContext, studentid, "Attendance/AttendancesStudentData");
                if (!IsValid)
                {
                    if(!status)
                        return RedirectToAction("Login", "Account");
                    return RedirectToAction("ManagerMenegarStudentView", "Menegar");
                }
            }
            else
            {

                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdStudent, IdSchool, status) = await _sessionValidatorService.ValidateStudentSessionAsync(HttpContext, studentid, "Attendance/AttendancesStudentData");
                if (!IsValid)
                {
                    if(!status)
                        return RedirectToAction("Login", "Account");
                    return RedirectToAction("Index", "Student");
                }
            }
            
            var name = _context.Students.FirstOrDefault(c => c.Id == studentid);
            ViewBag.name = name?.Name??"Null";
            ViewBag.IdStudent = Request.Query["studentid"];
            return View();
        }

        // GET: Grades/Edit/5
        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            Exception ex = new Exception();
            if (id == null)
            {
                int Id = HttpContext.Session.GetInt32("Id")??0;
                if(Id != 0){
                    _notyf.Error("The transmitted data cannot be tampered with.");
                    ex = new Exception("Manipulation of transmitted data");
                    await _logger.LogAsync(ex,"Grades/Edit");
                    return RedirectToAction("Index",new{teacherId =Id });
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _notyf.Error("Unauthenticated user.");
                ex = new Exception("Unauthenticated user.");
                await _logger.LogAsync(ex, "Grades/Edit");
                return RedirectToAction("Index","Home");
            }

            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
            {
                int Id = HttpContext.Session.GetInt32("Id")??0;
                if(Id != 0){
                    _notyf.Error("The transmitted data cannot be tampered with.");
                    ex = new Exception("Manipulation of transmitted data");
                    await _logger.LogAsync(ex,"Grades/Edit");
                    return RedirectToAction("Index",new{teacherId =Id });
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _notyf.Error("Unauthenticated user.");
                ex = new Exception("Unauthenticated user.");
                await _logger.LogAsync(ex, "Grades/Edit");
                return RedirectToAction("Index","Home");
            }
            return View(grade);
        }

        // POST: Grades/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("GradesId,FirstMonth,Mid,SecondMonth,Activity,Final")] Grade grade)
        {
            Exception ex = new Exception();
            if (id != grade.GradesId)
            {
                int Id = HttpContext.Session.GetInt32("Id")??0;
                if(Id != 0){
                    _notyf.Error("The transmitted data cannot be tampered with.");
                    ex = new Exception("Manipulation of transmitted data");
                    await _logger.LogAsync(ex,"Grades/Edit");
                    return RedirectToAction("Index",new{teacherId =Id });
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _notyf.Error("Unauthenticated user.");
                ex = new Exception("Unauthenticated user.");
                await _logger.LogAsync(ex, "Grades/Edit");
                return RedirectToAction("Index","Home");
            }
            var grades =await _context.Grades.FirstOrDefaultAsync(g => g.GradesId == grade.GradesId);
            if (grades == null)
            {
                int Id = HttpContext.Session.GetInt32("Id")??0;
                if(Id != 0){
                    _notyf.Error("The data sent is incorrect.");
                    ex = new Exception("The data sent is incorrect");
                    await _logger.LogAsync(ex,"Grades/Edit");
                    return RedirectToAction("Index",new{teacherId =Id });
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _notyf.Error("Unauthenticated user.");
                ex = new Exception("Unauthenticated user.");
                await _logger.LogAsync(ex, "Grades/Edit");
                return RedirectToAction("Index","Home");
            }
            if (ModelState.IsValid)
            {
                try
                {
                    grades.FirstMonth = grade.FirstMonth;
                    grades.Mid = grade.Mid;
                    grades.SecondMonth = grade.SecondMonth;
                    grades.Activity = grade.Activity;
                    grades.Final = grade.Final;
                    await _context.SaveChangesAsync();
                }
                catch (Exception exc)
                {
                    if(_context.Grades.Any(g => g.GradesId == id)){
                        int Idte = HttpContext.Session.GetInt32("Id")??0;
                        if(Idte != 0){
                            _notyf.Error("The transmitted data cannot be tampered with.");
                            ex = new Exception("Manipulation of transmitted data");
                            await _logger.LogAsync(exc,"Grades/Edit");
                            return RedirectToAction("Index",new{teacherId =Idte });
                    }
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _notyf.Error("Unauthenticated user.");
                    ex = new Exception("Unauthenticated user.");
                    await _logger.LogAsync(exc, "Grades/Edit");
                    return RedirectToAction("Index","Home");
                    }
                    int Id = HttpContext.Session.GetInt32("Id")??0;
                    if(Id != 0){
                        _notyf.Error("The transmitted data cannot be tampered with.");
                        ex = new Exception("Manipulation of transmitted data");
                        await _logger.LogAsync(exc,"Grades/Edit");
                        return RedirectToAction("Index",new{teacherId =Id });
                    }
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _notyf.Error("Unauthenticated user.");
                    ex = new Exception("Unauthenticated user.");
                    await _logger.LogAsync(exc, "Grades/Edit");
                    return RedirectToAction("Index","Home");
                }
                return RedirectToAction(nameof(Index),new{teacherId = grades.IdTeacher});
            }
            return View(grade);
        }

        // GET: Grades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.IdLectuerNavigation)
                .Include(g => g.IdStudentNavigation)
                .Include(g => g.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.GradesId == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // POST: Grades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            
            try{
                var grade = await _context.Grades.FindAsync(id);
                if (grade != null)
                {
                    int teacher = grade.IdTeacher??0;
                    _context.Grades.Remove(grade);
                    await _context.SaveChangesAsync();
                    _notyf.Success("The deletion process was completed successfully.");
                    return RedirectToAction("Index", new { idTeacher = teacher });
                }
                else
                {
                    int TeacherId = HttpContext.Session.GetInt32("Id")??0;
                    if(TeacherId != 0){
                        _notyf.Error("Data is not Found.");
                        return View(nameof(Index),new{idTeacher = TeacherId});
                    }
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _notyf.Error("Unauthenticated user.");
                    Exception ex = new Exception("Unauthenticated user.");
                    await _logger.LogAsync(ex, "Grades/Delete");
                    return RedirectToAction("Index","Home");
                }
                
            }catch(Exception ex){
                int TeacherId = HttpContext.Session.GetInt32("Id")??0;
                if(TeacherId != 0){
                    _notyf.Error("Data is not Found.");
                    await _logger.LogAsync(ex, "Grades/Delete");
                    return View(nameof(Index),new{idTeacher = TeacherId});
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _notyf.Error("Unauthenticated user.");
                await _logger.LogAsync(ex, "Grades/Delete");
                return RedirectToAction("Index","Home");
            }

        }

        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> GetSubjectsForTeacher(int teacherId)
        {
            Console.WriteLine($"Id Teacher: {teacherId}");
            var subjects = await _context.TeacherLectuerClasses
                .Where(ts => ts.IdTeacher == teacherId)
                .Include(l => l.IdLectuerNavigation)
                .Select(ts => new {
                    id = ts.IdLectuer,
                    name = ts.IdLectuerNavigation!=null? ts.IdLectuerNavigation.Name:"Null"
                }).ToListAsync();
            Console.WriteLine($"Count Lectuer: {subjects.Count()}");
            if (subjects.Count() <= 0)
            {
                _notyf.Error("There are no lectuers.");
            }
            return Json(subjects);
        }

        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> GetGradesForSubject(int teacherId, int subjectId)
        {
            var grades = await _context.TeacherLectuerClasses
                .Where(tg => tg.IdTeacher == teacherId)
                .Include(tg => tg.IdClassNavigation)
                .Select(tg => new {
                    id = tg.IdClass,
                    name = tg.IdClassNavigation!= null ? tg.IdClassNavigation.Name:"غير معرف"
                }).Distinct().ToListAsync();
            if(grades.Count()<=0){
                _notyf.Error("There are no lectuers.");
            }
            return Json(grades);
        }

       [AuthorizeRoles("Teacher")]
        private bool GradeExists(int id)
        {
            return _context.Grades.Any(e => e.GradesId == id);
        }
    }
}
