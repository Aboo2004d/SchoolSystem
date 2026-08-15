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
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Model;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;
        

        public AttendanceController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger, ISessionValidatorService sessionValidatorService)
        {
            _sessionValidatorService = sessionValidatorService;
            _context = context;
            _notyf = notyf;
            _logger = logger;
            _sessionValidatorService = sessionValidatorService;
        }
        // GET: Attendance
        
        [NonAction]
        public async Task<JsonResult> DataAttendance(
            Guid teacherId,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                Console.WriteLine($"Id TTTeacher: {teacherId}");
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, teacherId, "Attendance/DataAttendance");

                if (!IsValid)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }

                //فحص اذا كان تم ارسال قيمة المتغير ام لا و وصع قيمة افتراضية اذا كان لا
                if (length <= 0)
                    length = 10;
                length = Math.Min(length, 100);

                // تحديد قيمة الـ searchValue
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var attendanceQuery = _context.Attendances.Where(std =>
                    std.IdSchool == IdSchool && std.IdTeacher == IdTeacher &&
                    !std.IsDeletedAttendance &&
                    !std.IsDeletedStudent && !std.IsDeletedClass && !std.IsDeletedLectuer &&
                    !std.IsDeletedTeacher && !std.IsDeletedSchool);

                var totalRecords = await attendanceQuery.CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = attendanceQuery
                    .AsNoTracking()
                    .Select(s => new
                    {
                        id = s.Id,
                        StudentName = s.IdStudentNavigation != null ? s.IdStudentNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        excuse = s.Excuse ?? "Null",
                        Date = s.DateAndTime ?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                        Status = s.AttendanceStatus
                        
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.StudentName != null && s.StudentName.Contains(searchValue)) ||
                        (s.LectuerName != null && s.LectuerName.Contains(searchValue)) ||
                        (s.excuse != null && s.excuse.Contains(searchValue)) ||
                        (s.Date.ToString() != null && s.Date.ToString().Contains(searchValue)) ||
                        (s.ClassroomName != null &&s.ClassroomName.Contains(searchValue))
                    );
                }

                // عدد السجلات الاصلية التي تنطبق عليها الشروط
                var filteredCount = string.IsNullOrWhiteSpace(searchValue)
                    ? totalRecords
                    : await query.CountAsync();

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
                Select(s => new AttendanceViewModel
                {
                    Id = s.id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    LectuerName = s.LectuerName,
                    DateAndTime = s.Date,
                    Excuse = s.excuse,
                    AttendanceStatus = s.Status
                    })
                    .ToList();

                //ارجاع بيانات العرض اللازمة
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
        public async Task<IActionResult> ViewAttendance(Guid teacherId)
        {
            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, teacherId, "Grades/DataGrades");
            if (!IsValid)
            {
                
                return RedirectToAction("Index", "Teacher");
            }
            var name = await _context.Teachers.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == IdTeacher && c.IdSchool == IdSchool && !c.IsDeleted);
            ViewBag.name = name?.Name??"Null";
            ViewBag.IdTeacher = IdTeacher.ToString("D");
            return View();
        }

        // GET: Attendance/Create
        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Create(Guid? idLectuer, Guid? idTeacher, Guid? idClass)
        {
            idTeacher = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
            try{
                var (isValid, currentTeacherId, schoolId, teacherSessionIsActive) =
                    await _sessionValidatorService.ValidateTeacherSessionAsync(
                        HttpContext, idTeacher.Value, "Attendance/Create");
                if (!isValid)
                    return teacherSessionIsActive
                        ? RedirectToAction("Index", "Teacher")
                        : RedirectToAction("Login", "Account");

                if (idClass == null || idLectuer == null)
                {
                    
                    var (IsValid, IdTeacher, IdSchool, status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, idTeacher ?? Guid.Empty, "Attendance/Create");
                    if (!status)
                    {
                        return RedirectToAction("Login", "Account");
                    }

                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/Create");
                    return View(nameof(Index), new { idTeacher = idTeacher });
                }

                Lectuer? lec = await _context.Lectuers.AsNoTracking().FirstOrDefaultAsync(l =>
                    l.Id == idLectuer.Value && l.IdSchool == schoolId && !l.IsDeleted && !l.IsDeletedSchool);
                TheClass? cla = await _context.TheClasses.AsNoTracking().FirstOrDefaultAsync(c =>
                    c.Id == idClass.Value && c.IdSchool == schoolId && !c.IsDeleted && !c.IsDeletedSchool);
                
                if (lec == null || cla == null)
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/Create");
                    return View(nameof(Index), new { idTeacher = idTeacher });
                }

                TeacherLectuerClass? lectuer =await _context.TeacherLectuerClasses
                    .AsNoTracking()
                    .Where(tl => tl.IdTeacher == currentTeacherId && tl.IdSchool == schoolId &&
                        tl.IdLectuer == idLectuer && tl.IdClass == idClass &&
                        !tl.IsDeletedTeacherLectuerClass && !tl.IsDeletedTeacher &&
                        !tl.IsDeletedLectuer && !tl.IsDeletedClass && !tl.IsDeletedSchool &&
                        !tl.IsTeacherRemovedFromClass && !tl.IsTeacherRemovedFromLectuer)
                    .FirstOrDefaultAsync();
                    
                if (lectuer != null)
                {
                    ViewData["DateAndTime"] = DateOnly.FromDateTime(DateTime.Now);

                    ViewData["IdLectuer"] = idLectuer;
                    ViewData["IdTeacher"] = currentTeacherId;
                    ViewData["IdClass"] = idClass;

                    List<StudentLectuerTeacher>? student = await _context.StudentLectuerTeachers
                        .AsNoTracking()
                        .Where(t => t.IdTeacher == currentTeacherId && t.IdSchool == schoolId &&
                            t.IdClass == idClass && t.IdLectuer == idLectuer &&
                            !t.IsDeletedStudentLectuerTeacher && !t.IsDeletedStudent &&
                            !t.IsDeletedTeacher && !t.IsDeletedLectuer && !t.IsDeletedClass &&
                            !t.IsDeletedSchool && !t.IsTeacherRemovedFromClass &&
                            !t.IsTeacherRemovedFromLectuer)
                        .Include(st => st.IdStudentNavigation)
                        .ToListAsync();

                    if (student.Count == 0)
                    {
                        _notyf.Error("لا يوجد طلاب بعد.");
                        return View(nameof(Index), new { idTeacher = idTeacher });
                    }
                    ViewData["Students"] = student;

                    return View();

                }
                else
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/Create");
                    return View(nameof(Index), new { idTeacher = idTeacher });
                }
            }catch(Exception ex){
                if(idTeacher != Guid.Empty){
                    await _logger.LogAsync(ex,"Attendance/Create");
                    _notyf.Error("Data is not Found.");
                    return View(nameof(Index),new{idTeacher = idTeacher});
                }
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);

                await _logger.LogAsync(ex,"Attendance/Create");
                _notyf.Error("انتهت الجلسة.");
                return RedirectToAction("Index","Home");
            }
                
            
        }

        // POST: Attendance/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [NonAction]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        
        public async Task<IActionResult> Create(List<Attendance> Attendances)
        {
            try{
                if (Attendances.Count == 0)
                    return BadRequest();

                var requestedTeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "Attendance/Create");
                if (!isValid)
                    return Forbid();

                var lectureId = Attendances[0].IdLectuer;
                var classId = Attendances[0].IdClass;
                if (lectureId == null || classId == null)
                    return BadRequest();

                var ownsClassAndLecture = await _context.TeacherLectuerClasses.AsNoTracking().AnyAsync(t =>
                    t.IdTeacher == teacherId && t.IdSchool == schoolId &&
                    t.IdLectuer == lectureId && t.IdClass == classId &&
                    !t.IsDeletedTeacherLectuerClass && !t.IsDeletedTeacher &&
                    !t.IsDeletedLectuer && !t.IsDeletedClass && !t.IsDeletedSchool &&
                    !t.IsTeacherRemovedFromClass && !t.IsTeacherRemovedFromLectuer);
                if (!ownsClassAndLecture)
                    return Forbid();

                var postedStudentIds = Attendances
                    .Where(a => a.IdStudent.HasValue)
                    .Select(a => a.IdStudent!.Value)
                    .Distinct()
                    .ToList();
                if (postedStudentIds.Count != Attendances.Count)
                    return BadRequest();

                var allowedStudentIds = await _context.StudentLectuerTeachers.AsNoTracking()
                    .Where(t => t.IdTeacher == teacherId && t.IdSchool == schoolId &&
                        t.IdLectuer == lectureId && t.IdClass == classId &&
                        postedStudentIds.Contains(t.IdStudent!.Value) &&
                        !t.IsDeletedStudentLectuerTeacher && !t.IsDeletedStudent &&
                        !t.IsDeletedTeacher && !t.IsDeletedLectuer && !t.IsDeletedClass &&
                        !t.IsDeletedSchool && !t.IsTeacherRemovedFromClass &&
                        !t.IsTeacherRemovedFromLectuer)
                    .Select(t => t.IdStudent!.Value)
                    .Distinct()
                    .ToListAsync();
                if (allowedStudentIds.Count != postedStudentIds.Count)
                    return Forbid();

                var attendanceDate = DateOnly.FromDateTime(DateTime.Now);
                foreach (var attendance in Attendances)
                {
                    if (attendance.AttendanceStatus is not ("1" or "0" or "m"))
                        ModelState.AddModelError(nameof(Attendance.AttendanceStatus), "Invalid attendance status.");

                    attendance.Id = Guid.Empty;
                    attendance.IdTeacher = teacherId;
                    attendance.IdSchool = schoolId;
                    attendance.IdLectuer = lectureId;
                    attendance.IdClass = classId;
                    attendance.DateAndTime = attendanceDate;
                    if (attendance.AttendanceStatus != "m")
                        attendance.Excuse = null;
                }

                if (ModelState.IsValid){
                    await _context.Attendances.AddRangeAsync(Attendances);
                    await _context.SaveChangesAsync();
                    _notyf.Success("تم تسجيل الحضور و الغياب بنجاح");
                    return RedirectToAction("ViewAttendance", new { teacherId = Attendances[0].IdTeacher });
                }
                
                _notyf.Error("البيانات المدخلة غير صالحة.");
            }catch(Exception ex){
                _notyf.Error("حدث خطأ غير متوقع\nحاول مرة اخرى.");
                await _logger.LogAsync(ex,"Attendance/Create");
            }
            ViewData["DateAndTime"] = DateOnly.FromDateTime(DateTime.Now);

            ViewData["IdLectuer"] = Attendances[0].IdLectuer;
            ViewData["IdTeacher"] = Attendances[0].IdTeacher;
            ViewData["IdClass"] = Attendances[0].IdClass;

            List<StudentLectuerTeacher>? student = await _context.StudentLectuerTeachers
                .Where(t => t.IdTeacher == Attendances[0].IdTeacher && t.IdClass == Attendances[0].IdClass)
                .Include(st => st.IdStudentNavigation)
                .ToListAsync();

            ViewData["Students"] = student;
            return View(Attendances);
        }

        [NonAction]
        public async Task<IActionResult> GetLectuerForTeacher(Guid teacherId)
        {
            if(teacherId != HttpContext.Session.GetGuid("Id"))
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/GetLectuerForTeacher");
                return View(nameof(Index), new { idTeacher = teacherId });
            }
            var lectuer =_context.Attendances
                .Where(att=> att.DateAndTime == DateOnly.FromDateTime(DateTime.Now))
                .Select(att => att.IdLectuer)
                .FirstOrDefaultAsync();
            var subjects = await _context.TeacherLectuerClasses
                .Where(ts => ts.IdTeacher == teacherId && ts.IdLectuer != lectuer.Result)
                .Include(ts => ts.IdLectuerNavigation)
                .Select(ts => new {
                    id = ts.IdLectuerNavigation != null ? ts.IdLectuerNavigation.Id : Guid.Empty,
                    name = ts.IdLectuerNavigation!=null? ts.IdLectuerNavigation.Name:"غير معرف"
                }).ToListAsync();

            return Json(subjects);
        }

        [NonAction]
        public async Task<IActionResult> GetClassForSubject(Guid teacherId, Guid subjectId)
        {
            if(teacherId != HttpContext.Session.GetGuid("Id"))
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/GetClassForSubject");
                return View(nameof(Index), new { idTeacher = teacherId });
            }

            var TheClass =_context.Attendances
                .Where(att=> att.DateAndTime == DateOnly.FromDateTime(DateTime.Now))
                .Select(att => att.IdClass)
                .FirstOrDefaultAsync();
            var grades = await _context.TeacherLectuerClasses
                .Where(tg => tg.IdTeacher == teacherId && tg.IdClass != TheClass.Result)
                .Include(tg => tg.IdClassNavigation)
                .Select(tg => new {
                    id = tg.IdClassNavigation != null ? tg.IdClassNavigation.Id : Guid.Empty,
                    name = tg.IdClassNavigation!=null? tg.IdClassNavigation.Name:"غير معرف"
                }).ToListAsync();

            if(grades == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "Attendance/GetClassForSubject");
                return View(nameof(Index), new { idTeacher = teacherId });
            }
                
            return Json(grades);
        }

        [NonAction]
        public async Task<JsonResult> AttendancesStudentData(
            Guid studentid,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdStudent, IdSchool, status) = await _sessionValidatorService.ValidateStudentDataAccessAsync(HttpContext, studentid, "Attendance/AttendancesStudentData");
                if (!IsValid)
                {
                    return Json(new { success = false, status= status, error = "Unauthorized access. Session expired." });
                }

                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;

                // تحديد قيمة الـ searchValue
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.Attendances.Where(std => std.IdSchool == IdSchool && std.IdStudent == IdStudent)
                .Include(l => l.IdStudentNavigation)
                .Include(l => l.IdLectuerNavigation)
                .Include(l => l.IdClassNavigation)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Attendances.Where(std => std.IdSchool == IdSchool && std.IdStudent == IdStudent)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        id = s.Id,
                        StudentName = s.IdStudentNavigation != null ? s.IdStudentNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        excuse = s.Excuse ?? "Null",
                        Date = s.DateAndTime ?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
                        Status = s.AttendanceStatus
                        
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.StudentName != null && s.StudentName.Contains(searchValue)) ||
                        (s.LectuerName != null && s.LectuerName.Contains(searchValue)) ||
                        (s.excuse != null && s.excuse.Contains(searchValue)) ||
                        s.Date.ToString().Contains(searchValue) ||
                        (s.ClassroomName != null && s.ClassroomName.Contains(searchValue))
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
                    ("3", "asc") => query.OrderBy(s => s.excuse),
                    ("3", "desc") => query.OrderByDescending(s => s.excuse),
                    _ => query.OrderBy(s => s.StudentName)
                };

                // التقطيع (Pagination)
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                var students = data.
                Select(s => new AttendanceViewModel
                {
                    Id = s.id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    LectuerName = s.LectuerName,
                    DateAndTime = s.Date,
                    Excuse = s.excuse,
                    AttendanceStatus = s.Status
                    })
                    .ToList();

                // ارسال البيانات
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
                await _logger.LogAsync(e, "Attendance/AttendancesStudentData");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager, RoleNames.Student)]
        public async Task<IActionResult> AttendancesStudent(Guid studentid)
        {
            if (HttpContext.Session.GetString("Role") == "admin")
            {
                var (IsValid, IdStudent, IdSchool, status) = await _sessionValidatorService.ValidateStudentDataAccessAsync(HttpContext, studentid, "Attendance/AttendancesStudentData");
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
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
                var (IsValid, IdStudent, IdSchool, status) = await _sessionValidatorService.ValidateStudentDataAccessAsync(HttpContext, studentid, "Attendance/AttendancesStudentData");
                if (!IsValid)
                {
                    if(!status)
                        return RedirectToAction("Login", "Account");
                    return RedirectToAction("Index", "Student");
                }
            }
            Student? student =await _context.Students.Where(s => s.Id == studentid).Include(s=>s.IdClassNavigation).SingleOrDefaultAsync();
            ViewBag.StdClass = student?.IdClassNavigation?.Name ?? string.Empty;
            ViewBag.Role = HttpContext.Session.GetString("Role");
            ViewBag.StdId = Request.Query["studentid"];
            return View();
        }
        
        // GET: Attendance/Edit/5
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(Guid? id)
        {
            try{
                if (id == null)
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    return View(nameof(ViewAttendance));
                }

                var requestedTeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "Attendance/Edit");
                if (!isValid)
                    return Forbid();

                var attendance = await _context.Attendances.AsNoTracking().FirstOrDefaultAsync(a =>
                    a.Id == id.Value && a.IdTeacher == teacherId && a.IdSchool == schoolId &&
                    !a.IsDeletedTeacher && !a.IsDeletedSchool);
                if (attendance == null)
                {
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"),"Attendance/Edit");
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    return View(nameof(ViewAttendance));
                }
            
                if (attendance.IdStudent != null)
                {
                    var student = await _context.Students.AsNoTracking().FirstOrDefaultAsync(s =>
                        s.Id == attendance.IdStudent && s.IdSchool == schoolId && !s.IsDeletedStudent);
                    if (student == null)
                    {
                        Exception ex = new Exception("التلاعب بالبيانات المرسلة");
                        await _logger.LogAsync(ex,"Attendance/Edit");
                        _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                        return View(nameof(ViewAttendance));
                    }
                    ViewData["NameStudent"] =student.Name;
                    ViewData["IdLectuer"] =  attendance.IdLectuer;
                    ViewData["IdStudent"] = attendance.IdStudent;
                    ViewData["IdTeacher"] = attendance.IdTeacher;
                    ViewData["IdClass"] = attendance.IdClass;
                    ViewData["DateAndTime"] = attendance.DateAndTime;
                    ViewData["Status"] = new SelectList(new List<SelectListItem> {  
                        new SelectListItem { Text = "حضور", Value = "1" },
                        new SelectListItem { Text = "غياب", Value = "0" },
                        new SelectListItem { Text = "غياب بعذر", Value = "m" }
                    }, "Value", "Text", attendance.AttendanceStatus);
                    ViewData["Excuse"] = attendance.Excuse;
                    return View(attendance);
                }
                else
                {
                    Exception ex = new Exception("التلاعب بالبيانات المرسلة");
                    await _logger.LogAsync(ex,"Attendance/Edit");
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    return View(nameof(ViewAttendance));
                }
                
            }catch(Exception ex){
                await _logger.LogAsync(ex,"Index/SetCredentials");
                _notyf.Error("حدث خطأ غير متوقع\nحاول مرة اخرى لاحقا");
                return View(nameof(ViewAttendance));
            }

        }

        // POST: Attendance/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [NonAction]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,AttendanceStatus,Excuse")] Attendance attendance)
        {
            
            if (id != attendance.Id)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");

                return View(attendance);
            }

            var requestedTeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
            var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "Attendance/Edit");
            if (!isValid)
                return Forbid();

            var existingAttendance = await _context.Attendances.FirstOrDefaultAsync(a =>
                a.Id == id && a.IdTeacher == teacherId && a.IdSchool == schoolId &&
                !a.IsDeletedTeacher && !a.IsDeletedSchool);
            if (existingAttendance == null)
                return NotFound();

            if (attendance.AttendanceStatus == "1" || attendance.AttendanceStatus == "0" || attendance.AttendanceStatus == "m")
            {
                try
                {
                    existingAttendance.AttendanceStatus = attendance.AttendanceStatus;
                    existingAttendance.Excuse = attendance.AttendanceStatus == "m" ? attendance.Excuse : null;
                    await _context.SaveChangesAsync();
                    _notyf.Success("تمت عملية التعديل بنجاح");
                    return RedirectToAction("ViewAttendance", new { teacherId });

                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(ex, "Attendance/Edit");
                    _notyf.Error("حدث خطأ غير متوقع\nحاول مرة اخرى لاحقا");
                }
                
            }
            _notyf.Error("حالة الحضور غير صالحة");
            return RedirectToAction("Edit", new { id });
        }

        // GET: Attendance/Delete/5
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
                .ValidateTeacherSessionAsync(HttpContext, requestedTeacherId, "Attendance/DeletePage");
            if (!isValid)
                return Forbid();

            var attendance = await _context.Attendances
                .Include(a => a.IdLectuerNavigation)
                .Include(a => a.IdStudentNavigation)
                .Include(a => a.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id && m.IdTeacher == teacherId &&
                    m.IdSchool == schoolId && !m.IsDeletedAttendance &&
                    !m.IsDeletedTeacher && !m.IsDeletedSchool);
            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // POST: Attendance/Delete/5
        [HttpPost, ActionName("Delete")]
        [NonAction]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> DeleteConfirmed(Guid id) {
            try{
                var attendance = await _context.Attendances.FindAsync(id);
                if (attendance != null)
                {
                    Guid teacher = attendance.IdTeacher ?? Guid.Empty;
                    _context.Attendances.Remove(attendance);
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
                    await _logger.LogAsync(ex, "Attendance/DeleteConfirmed");
                    return RedirectToAction("Index","Home");
                }
            }catch(Exception ex){
                Guid TeacherId = HttpContext.Session.GetGuid("Id") ?? Guid.Empty;
                if (TeacherId != Guid.Empty) {
                    _notyf.Error("Data is not Found.");
                    await _logger.LogAsync(ex, "Attendance/DeleteConfirmed");
                    return View(nameof(Index),new{idTeacher = TeacherId});
                }
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                _notyf.Error("Unauthenticated user.");
                await _logger.LogAsync(ex, "Attendance/DeleteConfirmed");
                return RedirectToAction("Index","Home");
        }

            
        }

        private bool AttendanceExists(Guid id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }
    }
}
