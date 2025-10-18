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
        private readonly ISessionValidatorService _sessionValidatorService;
        private readonly EncryptionHelper _encryptionHelper;


        public GradesController(SystemSchoolDbContext context, INotyfService notyf, EncryptionHelper encryptionHelper, IErrorLoggerService logger, ISessionValidatorService sessionValidatorService)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
            _encryptionHelper = encryptionHelper;
        }
        // GET: Grades
        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> DataGrades(
            string? teacherId,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {

            int Id;

            try
            {
                Id = _encryptionHelper.DecryptInt(teacherId??"0");

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
                    Id = _encryptionHelper.EncryptInt(s.Id),
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    IdClass = _encryptionHelper.EncryptInt(s.idClass??0),
                    IdStudent = _encryptionHelper.EncryptInt(s.idStudent??0),
                    LectuerName = s.LectuerName,
                    IdTeacher = _encryptionHelper.EncryptInt(s.idTeacher??0),
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
        public async Task<IActionResult> ViewGrades(string? teacherId)
        {
            int Id;

            try
            {
                Id = _encryptionHelper.DecryptInt(teacherId??"0");

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }
            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, Id, "Grades/DataGrades");
            if (!IsValid)
            {
                
                return RedirectToAction("Index", "Teacher");
            }
            ViewBag.IdTeacher = teacherId;
            ViewBag.NameTeacher = _context.Teachers.FirstOrDefault(t => t.Id == Id)?.Name??"مجهول";

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
        public async Task<IActionResult> Create(string? teacherId, string? subjectId, string? gradeId)
        {
            
            int Id;
            int IdLectuer;
            int IdGrade;
            try
            {
                Id = _encryptionHelper.DecryptInt(teacherId ?? "0");
                IdLectuer = _encryptionHelper.DecryptInt(subjectId ?? "0");
                IdGrade = _encryptionHelper.DecryptInt(gradeId ?? "0");
                

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView", "Menegar");
            }
            
            if (gradeId == null || subjectId == null)
            {

                var (IsValid, IdTeacher, IdSchool, status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, Id, "Attendance/Create");
                if (!status)
                {
                    return RedirectToAction("Login", "Account");
                }

                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Grades/Create");
                return View(nameof(ViewGrades), new { idTeacher = teacherId });
            }

            bool isFind =   _context.Lectuers.Any(t => t.Id == IdLectuer)
                           && _context.TheClasses.Any(t => t.Id == IdGrade);
                           
            if (!isFind)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Grades/Create");
                return View(nameof(ViewGrades), new { teacherId = Id });
                
            }
            
            var students = _context.Students
                .Where(student =>
                _context.StudentLectuerTeachers.Any(stl => stl.IdClass == IdGrade
                && stl.IdLectuer == IdLectuer && stl.IdTeacher == Id)
                && _context.TeacherLectuerClasses.Any(tlc => tlc.IdClass == IdGrade && tlc.IdTeacher == Id)
                && student.IdClass == IdGrade
                )
                .ToList();

            var studentIds = students.Select(s=>(int?)s.Id).ToList();

            var existingGrades = _context.Grades
                .Where(g => g.IdTeacher == Id && g.IdLectuer == IdLectuer && studentIds.Contains(g.IdStudent))
                .Include(g => g.IdStudentNavigation)
                .ToList();

            var studentsWithGrades = students.Select(student =>
            {
                var grade = existingGrades.FirstOrDefault(g => g.IdStudent == student.Id);

                // إذا لم تكن هناك درجة، أنشئ واحدة جديدة بالقيم صفرية
                GradesViewModel gradesViewModel = new GradesViewModel();
                if (grade == null)
                {
                    gradesViewModel = new GradesViewModel
                    {
                        IdStudent = _encryptionHelper.EncryptInt(student.Id),
                        IdTeacher = teacherId,
                        IdLectuer = subjectId,
                        IdClass = gradeId,
                        FirstMonth = 0,
                        Mid = 0,
                        SecondMonth = 0,
                        Activity = 0,
                        Final = 0,
                        IdStudentNavigation = student
                    };
                }
                else
                {
                    gradesViewModel = new GradesViewModel
                    {
                        IdStudent = _encryptionHelper.EncryptInt(grade.IdStudent??0),
                        IdTeacher = teacherId,
                        IdLectuer = subjectId,
                        IdClass = gradeId,
                        FirstMonth = grade.FirstMonth,
                        Mid = grade.Mid,
                        SecondMonth = grade.SecondMonth,
                        Activity = grade.Activity,
                        Final = grade.Final,
                        IdStudentNavigation = student
                    };
                }

                return new { Student = student, Grade = gradesViewModel };
            }).ToList();
            string lectuer = _context.Lectuers.SingleOrDefault(l => l.Id == IdLectuer)?.Name??"غير معرف";
            string Classes = _context.TheClasses.SingleOrDefault(c => c.Id == IdGrade)?.Name??"غير معرف";
            ViewBag.StudentsWithGrades = studentsWithGrades;
            ViewBag.SubjectId = subjectId;
            ViewBag.SubjectName = lectuer;
            ViewBag.GradeName = Classes;
            ViewBag.ClassId = gradeId;
            ViewBag.TeacherId = teacherId;

            return View();
        }

        
        [HttpPost]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> SaveAll(List<GradeInputViewModel> Grades)
        {

            try
            {
                foreach (var item in Grades)
                {
                    int IdTeacher;
                    int IdLectuer;
                    int IdClass;
                    int IdStudent;
                    try
                    {

                        IdTeacher = _encryptionHelper.DecryptInt(item.TeacherId);
                        IdLectuer = _encryptionHelper.DecryptInt(item.LectuerId);
                        IdClass = _encryptionHelper.DecryptInt(item.ClassId);
                        IdStudent = _encryptionHelper.DecryptInt(item.StudentId);
                        
                    }
                    catch (Exception ex)
                    {
                        await _logger.LogAsync(ex, "Grades/ViewGrades");
                        _notyf.Error("حدث خطأ غير متوقع.");
                        return RedirectToAction("ViewGrades", "Grades", new { teacherId = item.TeacherId });
                    }

                    Student? std =await _context.Students.FirstOrDefaultAsync(s => s.Id == IdStudent);
                    bool lect =await _context.Lectuers.AnyAsync(s => s.Id == IdLectuer);
                    bool teach =await _context.Teachers.AnyAsync(s => s.Id == IdTeacher);
                    bool classes =await _context.TheClasses.AnyAsync(s => s.Id == IdClass);
                    if (std != null && lect && teach && classes)
                    {

                        Grade? grade =await _context.Grades
                            .FirstOrDefaultAsync(g => g.IdStudent == IdStudent && g.IdTeacher == IdTeacher && g.IdLectuer == IdLectuer && g.IdClass == IdClass);

                        if (grade == null)
                        {
                            grade = new Grade
                            {
                                IdStudent = IdStudent,
                                IdTeacher = IdTeacher,
                                IdLectuer = IdLectuer,
                                IdSchool = std.IdSchool,
                                IdClass = std.IdClass,
                                FirstMonth = item.FirstMonth,
                                Mid = item.Mid,
                                SecondMonth = item.SecondMonth,
                                Activity = item.Activity,
                                Final = item.Final

                            };
                            _context.Grades.Add(grade);
                        }
                        else
                        {
                            grade.FirstMonth = item.FirstMonth;
                            grade.Mid = item.Mid;
                            grade.SecondMonth = item.SecondMonth;
                            grade.Activity = item.Activity;
                            grade.Final = item.Final;
                            grade.IdClass = std.IdClass;
                            grade.IdSchool = std.IdSchool;

                        }
                    }
                    else
                    {
                        _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة!");
                    }
                    

                    
                }
                _context.SaveChanges();
                _notyf.Success("تم اضافة العلامات للطلاب بنجاح");
                return RedirectToAction("ViewGrades", new { teacherId = Grades.FirstOrDefault()?.TeacherId ?? "0" });

            }
            catch (Exception ex)
            {
                _notyf.Error("فشل تسجيل العلامات للطلاب\nحاول مرة اخرى لاحقا");
                await _logger.LogAsync(ex, "Grades/SaveAll");
                return RedirectToAction("ViewGrades", new { teacherId = Grades.FirstOrDefault()?.TeacherId ?? "0" });
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
                    Id = _encryptionHelper.EncryptInt(s.Id),
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    IdClass = _encryptionHelper.EncryptInt(s.idClass??0),
                    IdStudent = _encryptionHelper.EncryptInt(s.idStudent??0),
                    LectuerName = s.LectuerName,
                    IdTeacher = _encryptionHelper.EncryptInt(s.idTeacher??0),
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
        public async Task<IActionResult> Edit(string? id)
        {
            int Id = HttpContext.Session.GetInt32("Id")??0;
            if (id == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"),"Grades/Edit");
                return RedirectToAction("Index","Teacher");
                
            }

            int IdGrade;
            try
            {

                IdGrade = _encryptionHelper.DecryptInt(id);
                
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Grades/Edit");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ViewGrades", "Grades", new { teacherId = id });
            }

            var grade = await _context.Grades.FindAsync(IdGrade);
            if (grade == null)
            {
                if(Id != 0){
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"),"Grades/Edit");
                    return View(nameof(ViewGrades),new{teacherId =Id });
                }
                _notyf.Error("انتهت الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), "Grades/Edit");
                return RedirectToAction("Logout","Account");
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
            int Id = HttpContext.Session.GetInt32("Id")??0;
            if  (id != grade.GradesId)
            {
                if(Id != 0)
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"),"Grades/Edit");
                    return View(nameof(ViewGrades),new{teacherId =Id });
                }
                _notyf.Error("انتهت الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), "Grades/Edit");
                return RedirectToAction("Logout","Account");
            }

            var grades = await _context.Grades.FindAsync(id);
            if (grades == null)
            {
                if(Id != 0)
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"),"Grades/Edit");
                    return View(nameof(ViewGrades),new{teacherId =Id });
                }
                _notyf.Error("انتهت الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), "Grades/Edit");
                return RedirectToAction("Logout","Account");
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
                    if (_context.Grades.Any(g => g.GradesId == id))
                    {
                        if (Id != 0)
                        {
                            if (Id != 0)
                            {
                                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Grades/Edit");
                                return View(nameof(ViewGrades), new { teacherId = Id });
                            }
                        }
                        _notyf.Error("انتهت الجلسة.");
                        await _logger.LogAsync(new Exception("دخول غير مصرح."), "Grades/Edit");
                        return RedirectToAction("Logout","Account");
                    }
                    if (Id != 0)
                    {
                        _notyf.Error("The transmitted data cannot be tampered with.");
                        await _logger.LogAsync(new Exception("Manipulation of transmitted data"), "Grades/Edit");
                        return RedirectToAction("Index", new { teacherId = Id });
                    }
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _notyf.Error("Unauthenticated user.");
                    await _logger.LogAsync(new Exception("Unauthenticated user."), "Grades/Edit");
                    return RedirectToAction("Index", "Home");
                }
                return RedirectToAction(nameof(Index), new { teacherId = grades.IdTeacher });
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
        public async Task<IActionResult> GetSubjectsForTeacher(string? teacherId)
        {
            int Id;
            try
            {
                Id = _encryptionHelper.DecryptInt(teacherId ?? "0");
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/GetSubjectsForTeacher");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView", "Menegar");
            }

            Console.WriteLine($"Id Teacher Hyber: {teacherId}");

            Console.WriteLine($"Id Teacher: {Id}");

            var subjects = await _context.TeacherLectuerClasses
                .Where(ts => ts.IdTeacher == Id)
                .Include(l => l.IdLectuerNavigation)
                .Select(ts => new {
                    id = _encryptionHelper.EncryptInt(ts.IdLectuer??0),
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
        public async Task<IActionResult> GetGradesForSubject(string? teacherId, string subjectId)
        {

            int Id;
            try
            {
                Id = _encryptionHelper.DecryptInt(teacherId ?? "0");
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView", "Menegar");
            }


            Console.WriteLine($"Id Teacher Hyber1: {teacherId}");
            Console.WriteLine($"Id Teacher: {Id}");

            var grades = await _context.TeacherLectuerClasses
                .Where(tg => tg.IdTeacher == Id)
                .Include(tg => tg.IdClassNavigation)
                .Select(tg => new {
                    id = _encryptionHelper.EncryptInt(tg.IdClass??0),
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
