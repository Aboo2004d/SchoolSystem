using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;
using SchoolSystem.Controllers;

namespace SchoolSystem.Controllers
{
    public class MenegarController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        

        public MenegarController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
        }
        [AuthorizeRoles("admin")]
        // GET: Menegar
        public async Task<IActionResult> Index()
        {
            if(HttpContext.Session.GetString("Role") == "admin"){
                return View(await _context.Menegars.Where(m => m.Id == HttpContext.Session.GetInt32("Id")).ToListAsync());
            }
            return RedirectToAction("Login", "Account");
        }
        [AuthorizeRoles("admin")]
        // GET: Menegar/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menegar = await _context.Menegars
                .FirstOrDefaultAsync(m => m.Id == id);
            if (menegar == null)
            {
                return NotFound();
            }

            return View(menegar);
        }
        [AuthorizeRoles("admin")]
        // GET: Menegar/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Menegar/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Create([Bind("Name,Phone,Email")] Menegar menegar)
        {
            if (ModelState.IsValid)
            {
                _context.Add(menegar);
                await _context.SaveChangesAsync();
                _notyf.Success("Menegar created successfully!");
                return RedirectToAction(nameof(Index));
            }
            return View(menegar);
        }

        [AuthorizeRoles("admin")]
        // GET: Menegar/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menegar = await _context.Menegars.FindAsync(id);
            if (menegar == null)
            {
                return NotFound();
            }
            return View(menegar);
        }

        // POST: Menegar/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Name,Phone,Email")] Menegar menegar)
        {
            if (id != menegar.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(menegar);
                    await _context.SaveChangesAsync();
                    _notyf.Success("Menegar updated successfully!");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MenegarExists(menegar.Id))
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
            return View(menegar);
        }

        // GET: Menegar/Delete/5
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var menegar = await _context.Menegars
                .FirstOrDefaultAsync(m => m.Id == id);
            if (menegar == null)
            {
                return NotFound();
            }

            return View(menegar);
        }

        // POST: Menegar/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Console.WriteLine(1);
            var menegar = await _context.Menegars.FindAsync(id);
            if (menegar != null)
            {
                _context.Menegars.Remove(menegar);
                _notyf.Success("Menegar deleted successfully!");
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerMenegarStudent(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
            
        {
            try
            {
                
                int? idMenegar = HttpContext.Session.GetInt32("Id")??0;
                Console.WriteLine($"Session: {idMenegar}");
                if (idMenegar == 0)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                var menegar = await _context.Menegars.FindAsync(idMenegar);
                int? idSchool = menegar?.IdSchool;
                Console.WriteLine($"Menegar: {menegar.Name}");
                if(idSchool == 0){
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                Console.WriteLine($"School: {idSchool}");
                if (length <= 0)
                    length = 10;
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.Students
                .Where(std => std.IdSchool == idSchool)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Students.Where(std => std.IdSchool == idSchool && std.IsDeleted == false)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        ClassroomName = s.IdClassNavigation!=null? s.IdClassNavigation.Name:"Null",
                        Average = s.Grades.Select(g => g.Total).Average() ?? 0,
                        Day = s.Attendances.Count(att => att.AttendanceStatus == "1"),
                        TotalDay = s.Attendances.Count(),
                        address = s.City + "/" + s.Area
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.name != null && s.name.Contains(searchValue) )||
                        (s.ClassroomName != null && s.ClassroomName.Contains(searchValue)) ||
                        (s.address != null && s.address.Contains(searchValue)) ||
                        s.Average.ToString().Contains(searchValue)
                    );
                }

                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.name),
                    ("0", "desc") => query.OrderByDescending(s => s.name),
                    ("1", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("1", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    ("2", "asc") => query.OrderBy(s => s.Average),
                    ("2", "desc") => query.OrderByDescending(s => s.Average),
                    ("3", "asc") => query.OrderBy(s => s.Day),
                    ("3", "desc") => query.OrderByDescending(s => s.Day),
                    ("4", "asc") => query.OrderBy(s => s.address),
                    ("4", "desc") => query.OrderByDescending(s => s.address),
                    _ => query.OrderBy(s => s.name)
                };

                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                var students = data.
                Select(s => new MenegarStudentViewModel
                    {
                        IdStudent = s.id,
                        StudentName = s.name,
                        ClassroomName = s.ClassroomName,
                        Average = s.Average,
                        Day = s.Day,
                        TotalDay = s.TotalDay,
                        Address = s.address
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
                Console.WriteLine($"Error: {e.Message}");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("admin")]
        public IActionResult ManagerMenegarStudentView()
        {
            return View();
        }
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerMenegarTeacher(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                
                int? idMenegar = HttpContext.Session.GetInt32("Id")??0;
                Console.WriteLine($"Session: {idMenegar}");
                if (idMenegar == 0)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                var menegar = await _context.Menegars.FindAsync(idMenegar);
                int? idSchool = menegar?.IdSchool;
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
                var totalRecords = await _context.Teachers
                .Where(std => std.IdSchool == idSchool)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Teachers.Where(Teach => Teach.IdSchool == idSchool && Teach.IsDeleted == false)
                    .AsNoTracking()
                    .Select(t => new
                    {
                        id = t.Id,
                        name = t.Name,
                        phone = t.Phone,
                        email = t.Email,
                        address = t.City + "/" + t.Area
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(t =>
                        t.name != null && t.name.Contains(searchValue) ||
                        (t.phone != null && t.phone.Contains(searchValue)) ||
                        (t.address != null && t.address.Contains(searchValue)) ||
                        (t.email != null &&t.email.Contains(searchValue))
                    );
                }

                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.name),
                    ("0", "desc") => query.OrderByDescending(s => s.name),
                    ("1", "asc") => query.OrderBy(s => s.email),
                    ("1", "desc") => query.OrderByDescending(s => s.email),
                    ("2", "asc") => query.OrderBy(s => s.phone),
                    ("2", "desc") => query.OrderByDescending(s => s.phone),
                    _ => query.OrderBy(s => s.name)
                };
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                var teachers = data.
                Select(s => new MenegarTeacherViewModel
                {
                    Id = s.id,
                    Name = s.name,
                    Email = s.email,
                    Phone = s.phone,
                    Address = s.address
                    })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = teachers
                };
                

                return Json(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }

        [AuthorizeRoles("admin")]
        [HttpGet]
        public IActionResult ManagerMenegarTeacherView()
        {
            return View();
        }
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerMenegarClass(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
            
        {
            try
            {
                int? idMenegar = HttpContext.Session.GetInt32("Id")??0;
                if (idMenegar == 0)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                var menegar = await _context.Menegars.FindAsync(idMenegar);
                int? idSchool = menegar?.IdSchool;
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
                var totalRecords = await _context.TheClasses
                .Where(std => std.IdSchool == idSchool)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.TheClasses.Where(std => std.IdSchool == idSchool )
                    .AsNoTracking()
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        NumberOfStudents = s.Students.Where(std => std.IdClass == s.Id && s.IdSchool == idSchool)
                        .Select(sc => sc.Id).Distinct().Count(),

                        NumberOfTeacher = s.TeacherLectuerClasses.Select(sc => sc.IdTeacher).Distinct().Count()
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        s.name.Contains(searchValue)
                    );
                }

                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.name),
                    ("0", "desc") => query.OrderByDescending(s => s.name),
                    ("1", "asc") => query.OrderBy(s => s.NumberOfStudents),
                    ("1", "desc") => query.OrderByDescending(s => s.NumberOfStudents),
                    ("2", "asc") => query.OrderBy(s => s.NumberOfTeacher),
                    ("2", "desc") => query.OrderByDescending(s => s.NumberOfTeacher),
                    _ => query.OrderBy(s => s.name)
                };

                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                var students = data.
                Select(s => new MenegarClassViewModel
                    {
                        id = s.id,
                        ClassroomName = s.name,
                        NumberOfStudents = s.NumberOfStudents,
                        NumberOfTeacher = s.NumberOfTeacher
                    })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = students
                };
                Console.WriteLine($"Count Class: {students.Count()}");
                return Json(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("admin")]
        public IActionResult ManagerMenegarClassView()
        {
            return View();
        }
        
        /*[HttpGet]
        public async Task<IActionResult> ManagerMenegarStudentInClass([FromQuery] int idClass)
        {
            if (idClass == 0)
            {
                _notyf.Warning("Invalid class ID.");
                return RedirectToAction("ManagerMenegarClass", "Menegar");
            }

            try
            {

                var students = await _context.StudentLectuerTeachers.AsNoTracking()
                    .Where(sc => sc.IdClass == idClass)
                    .Select(ts => new ManagerMenegarStudentInClassViewModel
                    {
                        Id = ts.Id,
                        IdStudent = ts.IdStudentNavigation != null ? ts.IdStudentNavigation.Id : 0,
                        IdClass = ts.IdClassNavigation != null ? ts.IdClassNavigation.Id : 0,
                        StudentName = ts.IdStudentNavigation != null ? ts.IdStudentNavigation.Name : "Unknown",
                        ClassroomName = ts.IdClassNavigation != null ? ts.IdClassNavigation.Name : "Unknown"
                    })
                    .ToListAsync();

                // إذا لم يتم العثور على طلاب، إضافة صف فارغ مع IdClass
                if (!students.Any())
                {
                    students.Add(new ManagerMenegarStudentInClassViewModel
                    {
                        IdClass = idClass,
                    });
                }
                return View(students);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                await _logger.LogAsync(ex, $"An error occurred while fetching students for class ID: {idClass}");
                _notyf.Warning("Something went wrong while fetching the students.");
                return RedirectToAction("ManagerMenegarClass", "Menegar");
            }
        }*/
        [HttpGet]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerMenegarStudentInClass(
            int idClass,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
            
        {
            try
            {
                
                int? idMenegar = HttpContext.Session.GetInt32("Id")??0;
                if (idMenegar == 0)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                var menegar = await _context.Menegars.FindAsync(idMenegar);
                int? idSchool = menegar?.IdSchool;
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
                var totalRecords = await _context.TheClasses
                .Where(std => std.IdSchool == idSchool)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Students.Where(std => std.IdSchool == idSchool && std.IdClass == idClass && std.IsDeleted == false)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        idClass = s.IdClass
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.name!= null && s.name.Contains(searchValue))||
                        (s.ClassroomName!= null && s.ClassroomName.Contains(searchValue))
                    );
                }

                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.name),
                    ("0", "desc") => query.OrderByDescending(s => s.name),
                    ("1", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("1", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    _ => query.OrderBy(s => s.name)
                };

                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                var students = data.
                Select(s => new ManagerMenegarStudentInClassViewModel
                {
                    Id = s.id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.name,
                    IdClass = s.idClass
                    
                    })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = students
                };
                Console.WriteLine($"Count Stuent Class: {students.Count()}");
                return Json(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("admin")]
        public IActionResult ManagerMenegarStudentInClassView(int idClass)
        {
            ViewBag.IdClas = idClass;
            var name = _context.TheClasses.FirstOrDefault(c => c.Id == idClass);
            ViewBag.name = name?.Name ?? "Null";
            Console.WriteLine($"name student: {name.Name}");
            ViewBag.IdClass = Request.Query["idClass"];
            return View();
        }
        
         [HttpGet]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult>  ManagerMenegarTeacherInClass(
            int idClass,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
            
        {
            try
            {
                
                int? idMenegar = HttpContext.Session.GetInt32("Id")??0;
                if (idMenegar == 0)
                {
                    return Json(new { success = false, error = "Unauthorized access. Session expired." });
                }
                var menegar = await _context.Menegars.FindAsync(idMenegar);
                int? idSchool = menegar?.IdSchool;
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
                var totalRecords = await _context.TeacherLectuerClasses
                .Where(std => std.IdSchool == idSchool && std.IdClass == idClass)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.TeacherLectuerClasses.Where(tlc => tlc.IdSchool == idSchool && tlc.IdClass == idClass && tlc.IdTeacherNavigation.IsDeleted == false )
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        IdTeacher = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Id : 0,
                        IdClass = s.IdClassNavigation != null ? s.IdClassNavigation.Id : 0,
                        IdLectuer = s.IdClassNavigation != null ? s.IdClassNavigation.Id : 0,
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown"
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        s.TeacherName.Contains(searchValue)||
                        s.ClassroomName.Contains(searchValue)||
                        s.LectuerName.Contains(searchValue)
                    );
                }

                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.TeacherName),
                    ("0", "desc") => query.OrderByDescending(s => s.TeacherName),
                    ("1", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("1", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    ("2", "asc") => query.OrderBy(s => s.LectuerName),
                    ("2", "desc") => query.OrderByDescending(s => s.LectuerName),
                    _ => query.OrderBy(s => s.TeacherName)
                };

                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                var teacher = data.
                Select(s => new ManagerMenegarTeacherInClassViewModel
                    {
                        Id = s.Id,
                        ClassroomName = s.ClassroomName,
                        TeacherName = s.TeacherName,
                        LectuerName = s.LectuerName,
                        IdClass = s.IdClass,
                        IdTeacher = s.IdTeacher,
                        IdLectuer = s.IdLectuer
                    })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = teacher
                };
                Console.WriteLine($"Count Teacher Class: {teacher.Count()}");
                return Json(result);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("admin")]
        public IActionResult  ManagerMenegarTeacherInClassView(int idClass)
        {
            ViewBag.IdClas = idClass;
            var name = _context.TheClasses.FirstOrDefault(c => c.Id == idClass);
            ViewBag.name = name?.Name??"Null";
            ViewBag.IdClass = Request.Query["idClass"];
            return View();
        }

        public IActionResult GetStudentCountPerClass()
        {
            var data = _context.TheClasses.Where(c => c.IdSchool == HttpContext.Session.GetInt32("School") )
                .Select(c => new {
                    ClassName = c.Name,
                    StudentCount = c.Students.Where(sc => sc.IsDeleted == false).Count()
                }).ToList();

            return Json(data);
        }

        [HttpGet]
        public JsonResult GetTeacherCountPerSubject()
        {
            var data = _context.TeacherLectuerClasses.Where(t => t.IdSchool == HttpContext.Session.GetInt32("School") && t.IdTeacherNavigation.IsDeleted == false)
                .Include(t => t.IdLectuerNavigation) // تأكد من تضمين المادة
                .GroupBy(t => t.IdLectuerNavigation.Name)
                .Select(g => new
                {
                    subject = g.Key,
                    teacherCount = g.Select(x => x.IdTeacher).Distinct().Count()
                })
                .ToList();
                
            return Json(data);
        }

        [AuthorizeRoles("admin")]
        private bool MenegarExists(int id)
        {
            return _context.Menegars.Any(e => e.Id == id);
        }
    }
}
