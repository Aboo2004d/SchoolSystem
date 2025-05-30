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
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Model;

namespace SchoolSystem.Controllers
{
    public class AttendanceController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        

        public AttendanceController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
        }
        // GET: Attendance
        
        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> DataAttendance(
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
                var totalRecords = await _context.Attendances.Where(std => std.IdSchool == idSchool && std.IdTeacher == idTeacher)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Attendances.Where(std => std.IdSchool == idSchool && std.IdTeacher == idTeacher)
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
                        s.StudentName.Contains(searchValue) ||
                        s.LectuerName.Contains(searchValue) ||
                        s.excuse.Contains(searchValue) ||
                        s.Date.ToString().Contains(searchValue) ||
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
                    ("3", "asc") => query.OrderBy(s => s.excuse),
                    ("3", "desc") => query.OrderByDescending(s => s.excuse),
                    ("4", "asc") => query.OrderBy(s => s.Date),
                    ("4", "desc") => query.OrderByDescending(s => s.Date),
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
        public IActionResult ViewAttendance(int teacherId)
        {
            ViewBag.IdTeach = teacherId;
            var name = _context.Teachers.FirstOrDefault(c => c.Id == teacherId);
            ViewBag.name = name?.Name??"Null";
            ViewBag.IdTeacher = Request.Query["teacherId"];
            return View();
        }

        // GET: Attendance/Details/5
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Details(int? id){

            if (id == null)
            {
                _notyf.Error("You cannot go to this page without an ID.");
                return View(nameof(Index));
            }

            var attendance = await _context.Attendances
                .Include(a => a.IdLectuerNavigation)
                .Include(a => a.IdStudentNavigation)
                .Include(a => a.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attendance == null)
            {
                _notyf.Error("Invalid routing ID");
                return View(nameof(Index));
            }

            return View(attendance);
        }

        // GET: Attendance/Create
        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Create(int idLectuer, int idTeacher, int idClass)
        {
            try{

                Teacher tech =await _context.Teachers.FindAsync(idTeacher);
                Lectuer lec =await _context.Lectuers.FindAsync(idLectuer);
                TheClass cla  =await _context.TheClasses.FindAsync(idClass);
                if(tech == null || lec == null || cla == null){
                    int TeacherId = HttpContext.Session.GetInt32("Id")??0;
                    if(TeacherId != 0){
                        _notyf.Error("Data is not Found.");
                        return View(nameof(Index),new{idTeacher = TeacherId});
                    }
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    _notyf.Error("Unauthenticated user.");
                    return RedirectToAction("Index","Home");
                }
                var lectuer = _context.TeacherLectuerClasses
                    .Where(tl => tl.IdTeacher == idTeacher && tl.IdLectuer == idLectuer)
                    .FirstOrDefault();
                if (lectuer != null )
                {
                    var teacher = _context.TeacherLectuerClasses
                        .Where(tc => tc.IdTeacher == idTeacher && tc.IdClass == idClass)
                        .FirstOrDefault();
                    if (teacher != null){
                        ViewData["DateAndTime"] = DateOnly.FromDateTime(DateTime.Now);
                        ViewData["Status"] = new SelectList(new List<SelectListItem> {
                            new SelectListItem { Text = "Present", Value = "1" },
                            new SelectListItem { Text = "Absent", Value = "0" },
                            new SelectListItem { Text = "Excused", Value = "m" }
                        }, "Value", "Text");
                        ViewData["IdLectuer"] = idLectuer;
                        ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
                        ViewData["IdTeacher"] = idTeacher;
                        ViewData["IdClass"] = idClass;
                        var student = await _context.StudentLectuerTeachers
                            .Where(t => t.IdTeacher == lectuer.IdTeacher && t.IdTeacher == teacher.IdTeacher)
                            .Include(st => st.IdStudentNavigation) 
                            .ToListAsync();
                        
                        if (student == null)
                        {
                            _notyf.Error("Student not found.");
                            return View(nameof(Index));
                        }
                        ViewData["Students"] = student;
                        
                        return View();

                    }else{
                        _notyf.Error("Teacher not found.");
                        return View(nameof(Index));
                    }
                    
                }else {
                    _notyf.Error("Lectuer not found.");
                    return View(nameof(Index));
                    }
            }catch(Exception ex){
                int TeacherId = HttpContext.Session.GetInt32("Id")??0;
                if(TeacherId != 0){
                    await _logger.LogAsync(ex,"Attendance/Create");
                    _notyf.Error("Data is not Found.");
                    return View(nameof(Index),new{idTeacher = TeacherId});
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

                await _logger.LogAsync(ex,"Attendance/Create");
                _notyf.Error("Unauthenticated user.");
                return RedirectToAction("Index","Home");
            }
                
            
        }

        // POST: Attendance/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        
        public async Task<IActionResult> Create(List<Attendance> Attendances)
        {
            try{
                if (ModelState.IsValid){
                    foreach(var i in Attendances){
                        i.IdSchool = _context.Teachers.Find(i.IdTeacher)?.IdSchool??null;
                        await _context.AddRangeAsync(i);
                    }
                    await _context.SaveChangesAsync();
                    _notyf.Success("تم تسجيل الحضور و الغياب بنجاح");
                    return RedirectToAction("ViewAttendance", new { teacherId = Attendances[0].IdTeacher });
                }
                ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name");
                ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
                ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name");
                ViewData["DateAndTime"] = DateOnly.FromDateTime(DateTime.Now);
                ViewData["Status"] = new SelectList(new List<SelectListItem> {  
                        new SelectListItem { Text = "Present", Value = "1" },
                        new SelectListItem { Text = "Absent", Value = "0" },
                        new SelectListItem { Text = "Excused", Value = "m" }
                    }, "Value", "Text");
                _notyf.Error("The entered data is invalid.");
                return View(Attendances);
            }catch(Exception ex){
                ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name");
                ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
                ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name");
                ViewData["DateAndTime"] = DateOnly.FromDateTime(DateTime.Now);
                ViewData["Status"] = new SelectList(new List<SelectListItem> {
                        new SelectListItem { Text = "Present", Value = "1" },
                        new SelectListItem { Text = "Absent", Value = "0" },
                        new SelectListItem { Text = "Excused", Value = "m" }
                    }, "Value", "Text");
                _notyf.Error("Unexpected error.");
                await _logger.LogAsync(ex,"Attendance/Create");
                return View(Attendances);
            }

        }

        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> GetLectuerForTeacher(int teacherId)
        {
            var lectuer =_context.Attendances
                .Where(att=> att.DateAndTime == DateOnly.FromDateTime(DateTime.Now))
                .Select(att => att.IdLectuer)
                .FirstOrDefaultAsync();
            var subjects = await _context.TeacherLectuerClasses
                .Where(ts => ts.IdTeacher == teacherId && ts.IdLectuer != lectuer.Result)
                .Include(ts => ts.IdLectuerNavigation)
                .Select(ts => new {
                    id = ts.IdLectuerNavigation.Id,
                    name = ts.IdLectuerNavigation.Name
                }).ToListAsync();

            return Json(subjects);
        }

        public async Task<IActionResult> GetClassForSubject(int teacherId, int subjectId)
        {
            var TheClass =_context.Attendances
                .Where(att=> att.DateAndTime == DateOnly.FromDateTime(DateTime.Now))
                .Select(att => att.IdClass)
                .FirstOrDefaultAsync();
            var grades = await _context.TeacherLectuerClasses
                .Where(tg => tg.IdTeacher == teacherId && tg.IdClass != TheClass.Result)
                .Include(tg => tg.IdClassNavigation)
                .Select(tg => new {
                    id = tg.IdClass,
                    name = tg.IdClassNavigation.Name
                }).ToListAsync();
                foreach(var i in grades){
                    Console.WriteLine($"Lectuer: {i.name}");
                }
            return Json(grades);
        }

        [AuthorizeRoles("admin", "Student")]
        [HttpGet]
        public async Task<IActionResult> AttendancesStudentData(
            int studentid,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                
                int? idstudent = HttpContext.Session.GetInt32("Id")??0;
                if (idstudent == 0)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                Student? student = await _context.Students.FindAsync(idstudent);
                if(student == null){
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                int? idSchool = student.IdSchool;
                if(idSchool == null){
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
                var totalRecords = await _context.Attendances.Where(std => std.IdSchool == idSchool && std.IdStudent == studentid)
                .Include(l => l.IdStudentNavigation)
                .Include(l => l.IdLectuerNavigation)
                .Include(l => l.IdClassNavigation)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Attendances.Where(std => std.IdSchool == idSchool && std.IdStudent == studentid)
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
                    Console.WriteLine($"Count query Student Att: {query.Count()}");

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        s.StudentName.Contains(searchValue) ||
                        s.LectuerName.Contains(searchValue) ||
                        s.excuse.Contains(searchValue) ||
                        s.Date.ToString().Contains(searchValue) ||
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
                    ("3", "asc") => query.OrderBy(s => s.excuse),
                    ("3", "desc") => query.OrderByDescending(s => s.excuse),
                    ("4", "asc") => query.OrderBy(s => s.Date),
                    ("4", "desc") => query.OrderByDescending(s => s.Date),
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

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = students
                };
                Console.WriteLine($"Count Stuent Att: {students.Count()}");
                return Json(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [AuthorizeRoles("admin", "Student")]
        public async Task<IActionResult> AttendancesStudent(int studentid)
        {
            if(HttpContext.Session.GetString("Role") == "admin"){
                Student? student =await _context.Students.Where(s => s.Id == studentid).Include(s=>s.IdClassNavigation).FirstOrDefaultAsync();
                if(student != null){
                    ViewBag.StdClass = student.IdClassNavigation?.Name ?? "Null";
                    ViewBag.StdId = Request.Query["studentid"];
                    return View();
                }
                return RedirectToAction("ManagerMenegarStudentView","Menegar");

            }else{
                Student? student =await _context.Students.Where(s => s.Id == studentid).Include(s=>s.IdClassNavigation).FirstOrDefaultAsync();
                
                if (student != null)
                {
                    ViewBag.StdClass = student.IdClassNavigation?.Name ?? "Null";
                    ViewBag.StdId = Request.Query["studentid"];
                    return View();
                }
                return RedirectToAction("Index","Student");
            }
        }
        
        // GET: Attendance/Edit/5
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                return View(nameof(ViewAttendance));
            }

            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                Exception ex = new Exception("التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(ex,"Index/SetCredentials");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                return View(nameof(ViewAttendance));
            }
            try{
                if (attendance.IdStudent != null)
                {
                    var student = await _context.Students.FindAsync(attendance.IdStudent);
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
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AttendanceStatus,DateAndTime,Excuse,IdTeacher,IdLectuer,IdStudent,IdClass")] Attendance attendance)
        {
            
            if (id != attendance.Id)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");

                return View(attendance);
            }

            if (ModelState.IsValid)
            {
                try
                {
                    if (attendance.AttendanceStatus == "1" || attendance.AttendanceStatus == "0")
                    {
                        attendance.IdSchool = _context.Teachers.Find(attendance.IdTeacher)?.IdSchool ?? null;
                        attendance.Excuse = null;
                        _context.Update(attendance);
                        await _context.SaveChangesAsync();
                        _notyf.Success("تمت عملية التعديل بنجاح");
                        return RedirectToAction("ViewAttendance", new { idTeacher = attendance.IdTeacher });
                    }
                    else if (attendance.AttendanceStatus == "m")
                    {
                        attendance.IdSchool = _context.Teachers.Find(attendance.IdTeacher)?.IdSchool ?? null;
                        _context.Update(attendance);
                        await _context.SaveChangesAsync();
                        _notyf.Success("تمت عملية التعديل بنجاح");
                        return RedirectToAction("ViewAttendance", new { idTeacher = attendance.IdTeacher });
                    }
                    else
                    {
                        _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    }

                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(ex, "Attendance/Edit");
                    _notyf.Error("حدث خطأ غير متوقع\nحاول مرة اخرى لاحقا");
                }
                
            }
            return View(attendance);
        }

        // GET: Attendance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.IdLectuerNavigation)
                .Include(a => a.IdStudentNavigation)
                .Include(a => a.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // POST: Attendance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> DeleteConfirmed(int id) {
            try{
                var attendance = await _context.Attendances.FindAsync(id);
                if (attendance != null)
                {
                    int teacher = attendance.IdTeacher??0;
                    _context.Attendances.Remove(attendance);
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
                    await _logger.LogAsync(ex, "Attendance/DeleteConfirmed");
                    return RedirectToAction("Index","Home");
                }
            }catch(Exception ex){
                int TeacherId = HttpContext.Session.GetInt32("Id")??0;
                if(TeacherId != 0){
                    _notyf.Error("Data is not Found.");
                    await _logger.LogAsync(ex, "Attendance/DeleteConfirmed");
                    return View(nameof(Index),new{idTeacher = TeacherId});
                }
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                _notyf.Error("Unauthenticated user.");
                await _logger.LogAsync(ex, "Attendance/DeleteConfirmed");
                return RedirectToAction("Index","Home");
        }

            
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }
    }
}
