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
        

        public GradesController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
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
                
                int? idTeacher = HttpContext.Session.GetInt32("Id")??0;
                if (idTeacher == 0)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                var teacher = await _context.Teachers.FindAsync(idTeacher);
                int? idSchool = teacher?.IdSchool;
                if(idSchool == 0){
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                if (length <= 0)
                    length = 10;
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.Grades.Where(std => std.IdSchool == idSchool && std.IdTeacher == idTeacher)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Grades.Where(std => std.IdSchool == idSchool && std.IdTeacher == idTeacher)
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
                        s.StudentName.Contains(searchValue) ||
                        s.LectuerName.Contains(searchValue) ||
                        s.ClassroomName.Contains(searchValue)
                    );
                }

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
                Console.WriteLine($"Count Stuent Teacher: {students.Count()}");
                return Json(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public IActionResult ViewGrades(int teacherId)
        {
            ViewBag.IdTeach = teacherId;
            var name = _context.Teachers.FirstOrDefault(c => c.Id == teacherId);

            Console.WriteLine($"Teacher: {teacherId}");
            Console.WriteLine($"name Teacher: {name.Name}");
            ViewBag.name = name?.Name??"Null";
            ViewBag.IdTeacher = Request.Query["teacherId"];
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
                var lectuer = _context.Lectuers.FirstOrDefault(l => l.Id == subjectId).Name;
                var Classes = _context.TheClasses.FirstOrDefault(c => c.Id == gradeId).Name;
                ViewBag.StudentsWithGrades = studentsWithGrades;
                ViewBag.SubjectId = subjectId;
                ViewBag.SubjectName = lectuer;
                ViewBag.GradeName = Classes;
                ViewBag.GradeId = gradeId;
                ViewBag.TeacherId = teacherId;

            return View();
        }

        /*[HttpPost]
        [AuthorizeRoles("Teacher")]
        public IActionResult SaveMark(int studentId, string markType, int markValue, int teacherId, int subjectId)
        {
            // البحث عن السجل إذا كان موجود
            var grade = _context.Grades
                .FirstOrDefault(g => g.IdStudent == studentId && g.IdTeacher == teacherId && g.IdLectuer == subjectId);

            // إذا لم يكن موجودًا، يتم إنشاؤه
            if (grade == null)
            {
                grade = new Grade
                {
                    IdStudent = studentId,
                    IdTeacher = teacherId,
                    IdLectuer = subjectId,
                    FirstMonth = 0,
                    Mid = 0,
                    SecondMonth = 0,
                    Activity = 0,
                    Final = 0
                    // لا تضع Total هنا، لأنه يُحسب تلقائيًا من قاعدة البيانات
                };
                _context.Grades.Add(grade);
            }

            // تعديل القيمة حسب نوع العلامة
            switch (markType)
            {
                case "FirstMonth":
                    grade.FirstMonth = markValue;
                    break;
                case "Mid":
                    grade.Mid = markValue;
                    break;
                case "SecondMonth":
                    grade.SecondMonth = markValue;
                    break;
                case "Activity":
                    grade.Activity = markValue;
                    break;
                case "Final":
                    grade.Final = markValue;
                    break;
            }

            _context.SaveChanges();

            return Ok();
        }
        */
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
        
        /*[HttpGet]
        [AuthorizeRoles("Student","admin")]
        public async Task<IActionResult> MarkStudent(int studentid)
        {
            Exception ex = new Exception();
            string Role = HttpContext.Session.GetString("Role")??"Null";
            if(Role == "admin"){

                bool student = _context.Students.Any(t => t.Id ==studentid);
                if(!student){
                    
                    int Id = HttpContext.Session.GetInt32("Id")??0;
                        if(Id != 0){
                            _notyf.Error("The transmitted data cannot be tampered with.");
                            ex = new Exception("Manipulation of transmitted data");
                            await _logger.LogAsync(ex,"Grades/MarkStudent");
                            return RedirectToAction("managerMenegarStudentView","Menegar");
                        }
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        _notyf.Error("Unauthenticated user.");
                        ex = new Exception("Unauthenticated user.");
                        await _logger.LogAsync(ex, "Grades/MarkStudent");
                        return RedirectToAction("Index","Home");
                }
                var systemSchoolDbContext = _context.Grades.Where(g => g.IdStudent == studentid).Include(g => g.IdLectuerNavigation).Include(g => g.IdStudentNavigation).Include(g => g.IdTeacherNavigation);
                return View(await systemSchoolDbContext.ToListAsync());
            }else if(Role == "Student"){
                bool student = _context.Students.Any(t => t.Id ==studentid);
                if(!student){
                    int Id = HttpContext.Session.GetInt32("Id")??0;
                        if(Id != 0){
                            _notyf.Error("The transmitted data cannot be tampered with.");
                            ex = new Exception("Manipulation of transmitted data");
                            await _logger.LogAsync(ex,"Grades/MarkStudent");
                            return RedirectToAction("Inedx","Student");
                        }
                        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                        _notyf.Error("Unauthenticated user.");
                        ex = new Exception("Unauthenticated user.");
                        await _logger.LogAsync(ex, "Grades/MarkStudent");
                        return RedirectToAction("Index","Home");
                }
                var systemSchoolDbContext = _context.Grades.Where(g => g.IdStudent == studentid).Include(g => g.IdLectuerNavigation).Include(g => g.IdStudentNavigation).Include(g => g.IdTeacherNavigation);
                return View(await systemSchoolDbContext.ToListAsync());
            }
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _notyf.Error("Unauthenticated user.");
            ex = new Exception("Unauthenticated user.");
            await _logger.LogAsync(ex, "Grades/MarkStudent");
            return RedirectToAction("Index","Home");
        }
        */
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
                
                int? idTeacher = HttpContext.Session.GetInt32("Id")??0;
                if (idTeacher == 0)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                var student = await _context.Students.FindAsync(studentid);
                int? idSchool = student?.IdSchool;
                if(idSchool == 0){
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                if (length <= 0)
                    length = 10;
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.Grades.Where(std => std.IdSchool == idSchool && std.IdStudent == studentid)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Grades.Where(std => std.IdSchool == idSchool && std.IdStudent == studentid)
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
                        s.StudentName.Contains(searchValue) ||
                        s.LectuerName.Contains(searchValue) ||
                        s.ClassroomName.Contains(searchValue)
                    );
                }

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
                Console.WriteLine($"Count Stuent Teacher: {students.Count()}");
                return Json(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("Student","admin")]
        public IActionResult MarkStudent(int studentid)
        {
            ViewBag.IdStd = studentid;
            var name = _context.Students.FirstOrDefault(c => c.Id == studentid);
            ViewBag.name = name?.Name??"Null";
            ViewBag.IdStudent = Request.Query["studentid"];
            return View();
        }


        // POST: Grades/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        
        /*
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Create([Bind("GradesId,FirstMonth,Mid,SecondMonth,Activity,Final,Total,IdStudent,IdTeacher,IdLectuer")] Grade grade)
        {
            if (ModelState.IsValid)
            {
                _context.Add(grade);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name", grade.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name", grade.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name", grade.IdTeacher);
            return View(grade);
        }*/

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
                    name = tg.IdClassNavigation.Name
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
