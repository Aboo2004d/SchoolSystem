using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
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


        public GradesController(SystemSchoolDbContext context, INotyfService notyf, IErrorLoggerService logger, ISessionValidatorService sessionValidatorService)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
        }
        // GET: Grades
        [NonAction]
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
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> ViewGrades(Guid? teacherId)
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
        public async Task<IActionResult> Details(Guid? id)
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
        public async Task<IActionResult> Create(Guid? teacherId, Guid? subjectId, Guid? gradeId)
        {
            
            Guid Id;
            Guid IdLectuer;
            Guid IdGrade;
            try
            {
                Id = teacherId ?? Guid.Empty;
                IdLectuer = subjectId ?? Guid.Empty;
                IdGrade = gradeId ?? Guid.Empty;
                

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView", "Menegar");
            }

            var (teacherAccessIsValid, currentTeacherId, schoolId, teacherSessionIsActive) =
                await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, Id, "Grades/Create");
            if (!teacherAccessIsValid)
                return teacherSessionIsActive
                    ? Forbid()
                    : RedirectToAction("Login", "Account");
            Id = currentTeacherId;
            
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

            bool isFind = await _context.TeacherLectuerClasses.AsNoTracking().AnyAsync(t =>
                t.IdTeacher == Id && t.IdSchool == schoolId && t.IdLectuer == IdLectuer &&
                t.IdClass == IdGrade && !t.IsDeletedTeacherLectuerClass &&
                !t.IsDeletedTeacher && !t.IsDeletedLectuer && !t.IsDeletedClass &&
                !t.IsDeletedSchool && !t.IsTeacherRemovedFromClass &&
                !t.IsTeacherRemovedFromLectuer);
                           
            if (!isFind)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Grades/Create");
                return View(nameof(ViewGrades), new { teacherId = Id });
                
            }
            
            var students = await _context.StudentLectuerTeachers.AsNoTracking()
                .Where(stl => stl.IdTeacher == Id && stl.IdSchool == schoolId &&
                    stl.IdClass == IdGrade && stl.IdLectuer == IdLectuer &&
                    !stl.IsDeletedStudentLectuerTeacher && !stl.IsDeletedStudent &&
                    !stl.IsDeletedTeacher && !stl.IsDeletedLectuer && !stl.IsDeletedClass &&
                    !stl.IsDeletedSchool && !stl.IsTeacherRemovedFromClass &&
                    !stl.IsTeacherRemovedFromLectuer && stl.IdStudentNavigation != null)
                .Select(stl => stl.IdStudentNavigation!)
                .Distinct()
                .ToListAsync();

            var studentIds = students.Select(s => (Guid?)s.Id).ToList();

            var existingGrades = _context.Grades
                .Where(g => g.IdTeacher == Id && g.IdSchool == schoolId &&
                    g.IdLectuer == IdLectuer && g.IdClass == IdGrade &&
                    studentIds.Contains(g.IdStudent) && !g.IsDeletedGrades)
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
                        IdStudent = student.Id,
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
                        IdStudent = grade.IdStudent ?? Guid.Empty,
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
        [NonAction]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> SaveAll(List<GradeInputViewModel> Grades)
        {

            try
            {
                foreach (var item in Grades)
                {
                    Guid IdTeacher;
                    Guid IdLectuer;
                    Guid IdClass;
                    Guid IdStudent;
                    try
                    {

                        IdTeacher = item.TeacherId;
                        IdLectuer = item.LectuerId;
                        IdClass = item.ClassId;
                        IdStudent = item.StudentId;
                        
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
                return RedirectToAction("ViewGrades", new { teacherId = Grades.FirstOrDefault()?.TeacherId ?? Guid.Empty });

            }
            catch (Exception ex)
            {
                _notyf.Error("فشل تسجيل العلامات للطلاب\nحاول مرة اخرى لاحقا");
                await _logger.LogAsync(ex, "Grades/SaveAll");
                return RedirectToAction("ViewGrades", new { teacherId = Grades.FirstOrDefault()?.TeacherId ?? Guid.Empty });
            }
        }
        
        [NonAction]
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
                var (IsValid, IdStudent, IdSchool,status) = await _sessionValidatorService.ValidateStudentDataAccessAsync(HttpContext, studentid, "Grades/DataGradesStudent");
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


        [HttpGet]
        [AuthorizeRoles(RoleNames.Student, RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> MarkStudent(Guid studentid)
        {
            if (HttpContext.Session.GetString("Role") == "admin")
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdStudent, IdSchool, status) = await _sessionValidatorService.ValidateStudentDataAccessAsync(HttpContext, studentid, "Grades/MarkStudent");
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
                var (IsValid, IdStudent, IdSchool, status) = await _sessionValidatorService.ValidateStudentDataAccessAsync(HttpContext, studentid, "Grades/MarkStudent");
                if (!IsValid)
                {
                    if(!status)
                        return RedirectToAction("Login", "Account");
                    return RedirectToAction("Index", "Student");
                }
            }
            
            var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(c =>
                c.Id == studentid && !c.IsDeletedStudent && !c.IsDeletedSchool);
            if (student == null)
                return NotFound();
            ViewBag.name = student.Name ?? "غير معرف";
            ViewBag.IdStudent = studentid.ToString("D");
            return View();
        }

        // GET: Grades/Edit/5
        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            Guid Id = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
            var (isValid, teacherId, schoolId, sessionIsActive) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, Id, "Grades/Edit");
            if (!isValid)
                return sessionIsActive
                    ? RedirectToAction(nameof(ViewGrades), new { teacherId = Id })
                    : RedirectToAction("Login", "Account");

            if (id == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"),"Grades/Edit");
                return RedirectToAction("Index","Teacher");
                
            }

            Guid IdGrade;
            try
            {

                IdGrade = id ?? Guid.Empty;
                
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Grades/Edit");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ViewGrades", "Grades", new { teacherId = id });
            }

            var grade = await _context.Grades.AsNoTracking().FirstOrDefaultAsync(g =>
                g.GradesId == IdGrade && g.IdTeacher == teacherId && g.IdSchool == schoolId &&
                !g.IsDeletedGrades && !g.IsDeletedTeacher && !g.IsDeletedSchool);
            if (grade == null)
            {
                if(Id != Guid.Empty){
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
        [NonAction]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(Guid id, [Bind("GradesId,FirstMonth,Mid,SecondMonth,Activity,Final")] Grade grade)
        {
            Guid Id = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
            var (isValid, teacherId, schoolId, sessionIsActive) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, Id, "Grades/Edit");
            if (!isValid)
                return sessionIsActive
                    ? RedirectToAction(nameof(ViewGrades), new { teacherId = Id })
                    : RedirectToAction("Login", "Account");

            if  (id != grade.GradesId)
            {
                if(Id != Guid.Empty)
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"),"Grades/Edit");
                    return View(nameof(ViewGrades),new{teacherId =Id });
                }
                _notyf.Error("انتهت الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), "Grades/Edit");
                return RedirectToAction("Logout","Account");
            }

            var grades = await _context.Grades.FirstOrDefaultAsync(g =>
                g.GradesId == id && g.IdTeacher == teacherId && g.IdSchool == schoolId &&
                !g.IsDeletedGrades && !g.IsDeletedTeacher && !g.IsDeletedSchool);
            if (grades == null)
            {
                if(Id != Guid.Empty)
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
                        if (Id != Guid.Empty)
                        {
                            if (Id != Guid.Empty)
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
                    if (Id != Guid.Empty)
                    {
                        _notyf.Error("The transmitted data cannot be tampered with.");
                        await _logger.LogAsync(new Exception("Manipulation of transmitted data"), "Grades/Edit");
                        return RedirectToAction(nameof(ViewGrades), new { teacherId = Id });
                    }
                    await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                    _notyf.Error("Unauthenticated user.");
                    await _logger.LogAsync(new Exception("Unauthenticated user."), "Grades/Edit");
                    return RedirectToAction("Index", "Home");
                }
                return RedirectToAction(nameof(ViewGrades), new { teacherId = grades.IdTeacher ?? Id });
            }
            grade.IdTeacher = grades.IdTeacher ?? Id;
            return View(grade);
        }

        // GET: Grades/Delete/5
        [HttpGet]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var requestedTeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
            var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "Grades/DeletePage");
            if (!isValid)
                return Forbid();

            var grade = await _context.Grades
                .Include(g => g.IdLectuerNavigation)
                .Include(g => g.IdStudentNavigation)
                .Include(g => g.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.GradesId == id && m.IdTeacher == teacherId &&
                    m.IdSchool == schoolId && !m.IsDeletedGrades &&
                    !m.IsDeletedTeacher && !m.IsDeletedSchool);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // POST: Grades/Delete/5
        [HttpPost, ActionName("Delete")]
        [NonAction]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            
            try{
                var grade = await _context.Grades.FindAsync(id);
                if (grade != null)
                {
                    Guid teacher = grade.IdTeacher ?? Guid.Empty;
                    _context.Grades.Remove(grade);
                    await _context.SaveChangesAsync();
                    _notyf.Success("The deletion process was completed successfully.");
                    return RedirectToAction("Index", new { idTeacher = teacher });
                }
                else
                {
                    Guid TeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
                    if (TeacherId != Guid.Empty) {
                        _notyf.Error("Data is not Found.");
                        return View(nameof(Index),new{idTeacher = TeacherId});
                    }
                    await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                    _notyf.Error("Unauthenticated user.");
                    Exception ex = new Exception("Unauthenticated user.");
                    await _logger.LogAsync(ex, "Grades/Delete");
                    return RedirectToAction("Index","Home");
                }
                
            }catch(Exception ex){
                Guid TeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
                if (TeacherId != Guid.Empty) {
                    _notyf.Error("Data is not Found.");
                    await _logger.LogAsync(ex, "Grades/Delete");
                    return View(nameof(Index),new{idTeacher = TeacherId});
                }
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                _notyf.Error("Unauthenticated user.");
                await _logger.LogAsync(ex, "Grades/Delete");
                return RedirectToAction("Index","Home");
            }

        }

        

        [NonAction]
        public async Task<IActionResult> GetSubjectsForTeacher(Guid? teacherId)
        {
            Guid Id;
            try
            {
                Id = teacherId ?? Guid.Empty;
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
                    id = ts.IdLectuer ?? Guid.Empty,
                    name = ts.IdLectuerNavigation!=null? ts.IdLectuerNavigation.Name:"Null"
                }).ToListAsync();
            Console.WriteLine($"Count Lectuer: {subjects.Count()}");
            if (subjects.Count() <= 0)
            {
                _notyf.Error("There are no lectuers.");
            }
            return Json(subjects);
        }

        [NonAction]
        public async Task<IActionResult> GetGradesForSubject(Guid? teacherId, Guid subjectId)
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
                return RedirectToAction("ManagerMenegarTeacherView", "Menegar");
            }


            Console.WriteLine($"Id Teacher Hyber1: {teacherId}");
            Console.WriteLine($"Id Teacher: {Id}");

            var grades = await _context.TeacherLectuerClasses
                .Where(tg => tg.IdTeacher == Id)
                .Include(tg => tg.IdClassNavigation)
                .Select(tg => new {
                    id = tg.IdClass ?? Guid.Empty,
                    name = tg.IdClassNavigation!= null ? tg.IdClassNavigation.Name:"غير معرف"
                }).Distinct().ToListAsync();
            if(grades.Count()<=0){
                _notyf.Error("There are no lectuers.");
            }
            return Json(grades);
        }

       [AuthorizeRoles("Teacher")]
        private bool GradeExists(Guid id)
        {
            return _context.Grades.Any(e => e.GradesId == id);
        }
    }
}
