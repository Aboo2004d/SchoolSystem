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
        private readonly ISessionValidatorService _sessionValidatorService;


        public MenegarController(SystemSchoolDbContext context, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        // GET: Menegar
        public  IActionResult Index()
        {
            return View();
        }
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        // GET: Menegar/Details/5
        public async Task<IActionResult> Details(Guid? id)
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> Create([Bind("Name,Phone,Email, TheDate, IdNumber")] Menegar menegar)
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

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        // GET: Menegar/Edit/5
        public async Task<IActionResult> Edit(Guid? id)
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> Edit(Guid id, [Bind("Name,Phone,Email")] Menegar menegar)
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> Delete(Guid? id)
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
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

        
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> ManagerMenegarStudent(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
            
        {
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Menegar/ManagerMenegarStudent");
                if (!IsValid)
                {
                    return Json(new { success = false, message = Message, error = "Unauthorized access. Session expired." });
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
                var totalRecords = await _context.Students
                .Where(std => std.IdSchool == IdSchool)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Students.Where(std => std.IdSchool == IdSchool && std.IsDeletedStudent == false)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        ClassroomName = s.IdClassNavigation == null 
                        ? "فارغ" 
                        : s.IdClassNavigation.IsDeleted == true
                            ? s.IdClassNavigation.Name + " (صف محذوف)" 
                            : s.IdClassNavigation.Name,
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

                // الحصول على القيم بعد الفلترة
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
                
                //تقطيع
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // ارسال بيانات للعرض
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
                await _logger.LogAsync(e, "Menegar/ManagerMenegarStudent");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public IActionResult ManagerMenegarStudentView()
        {
            return View();
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> ManagerMenegarTeacher(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Lectuer/ManagerMenegarTeacher");
                if (!IsValid)
                {
                    return Json(new { success = false, message = Message, error = "Unauthorized access. Session expired." });
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
                var totalRecords = await _context.Teachers
                .Where(std => std.IdSchool == IdSchool && std.IsDeleted == false)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Teachers.Where(Teach => Teach.IdSchool == IdSchool && Teach.IsDeleted == false)
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

                // الحصول على القيم بعد الفلترة
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

                //تقطيع
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // ارسال بيانات للعرض
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
                await _logger.LogAsync(e, "Menegar/ManagerMenegarTeacher");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet]
        public IActionResult ManagerMenegarTeacherView()
        {
            return View();
        }

        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public IActionResult ManagerMenegarClassView()
        {
            return View();
        }
        
        
        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> ManagerMenegarStudentInClass(
            Guid idClass,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
            
        {
            try
            {
                Guid Id;

                try
                {
                    Id = idClass;
                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(ex, "TheClass/Edit");
                    _notyf.Error("حدث خطأ غير متوقع.");
                    return View(nameof(ManagerMenegarStudentInClassView));
                }
                
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Lectuer/ManagerMenegarClass");
                if (!IsValid)
                {
                    return Json(new { success = false, message = Message, error = "Unauthorized access. Session expired." });
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
                var totalRecords = await _context.Students.Where(std => std.IdSchool == IdSchool && std.IdClass == Id && std.IsDeletedStudent == false)
                .Where(std => std.IdSchool == IdSchool)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Students.Where(std => std.IdSchool == IdSchool && std.IdClass == Id && std.IsDeletedStudent == false)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        id = s.Id,
                        name = s.Name,
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        idClass = s.IdClass ?? Guid.Empty
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.name!= null && s.name.Contains(searchValue))||
                        (s.ClassroomName!= null && s.ClassroomName.Contains(searchValue))
                    );
                }

                // الحصول على القيم بعد الفلترة
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

                // تقطيع
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // ارسال بيانات للعرض
                var students = data.
                Select(s => new ManagerMenegarStudentInClassViewModel
                {
                    Id = s.id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.name,
                    IdClass = s.idClass
                    
                    })
                    .ToList();

                Console.WriteLine($"Count123: {students.Count()}");

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
                await _logger.LogAsync(e, "Menegar/ManagerMenegarStudentInClass");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> ManagerMenegarStudentInClassView(Guid idClass)
        {
            if (idClass == null)
            {
                await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "TheClass/Edit");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ");
                return RedirectToAction("ManagerMenegarClassView", "Menegar");
            }

            Guid Id;

            try
            {
                Id = idClass;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Menegar/ManagerMenegarStudentInClassView");
                _notyf.Error("حدث خطأ غير متوقع.");
                return View();
            }

            TheClass? theClass = await _context.TheClasses.SingleOrDefaultAsync(c => c.Id == Id);
            if (theClass == null)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));
                return View();
            }
            ViewBag.name = theClass?.Name ?? "Null";
            ViewBag.IdClass = Request.Query["idClass"];
            return View();
        }
        
         [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult>  ManagerMenegarTeacherInClass(
            Guid idClass,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
            
        {
            try
            {
                Guid Id;

                try
                {
                    Id = idClass;

                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(ex, "Menegar/ManagerMenegarStudentInClassView");
                    _notyf.Error("حدث خطأ غير متوقع.");
                    return View();
                }
                
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Lectuer/ManagerMenegarClass");
                if (!IsValid)
                {
                    return Json(new { success = false, message = Message, error = "Unauthorized access. Session expired." });
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
                var totalRecords = await _context.TeacherLectuerClasses
                .Where(std => std.IdSchool == IdSchool && std.IdClass == Id)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.TeacherLectuerClasses.Where(tlc => tlc.IdSchool == IdSchool && tlc.IdClass == Id && tlc.IdTeacherNavigation.IsDeleted == false )
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        IdTeacher = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Id : Guid.Empty,
                        IdClass = s.IdClassNavigation != null ? s.IdClassNavigation.Id : Guid.Empty,
                        IdLectuer = s.IdClassNavigation != null ? s.IdClassNavigation.Id : Guid.Empty,
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown"
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.TeacherName != null && s.TeacherName.Contains(searchValue))||
                        (s.ClassroomName != null && s.ClassroomName.Contains(searchValue))||
                        (s.LectuerName != null && s.LectuerName.Contains(searchValue))
                    );
                }

                // الحصول على القيم بعد الفلترة
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

                // تقطيع
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // ارسال البيانات للعرض
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
                
                return Json(result);
            }
            catch (Exception e)
            {
                await _logger.LogAsync(e, "Menegar/ManagerMenegarTeacherInClass");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult>  ManagerMenegarTeacherInClassView(Guid idClass)
        {

            Guid Id;

            try
            {
                Id = idClass;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Menegar/ManagerMenegarStudentInClassView");
                _notyf.Error("حدث خطأ غير متوقع.");
                return View();
            }

            var (isValid, school, _) = await _sessionValidatorService
                .ValidateManagerSessionAsync(HttpContext, "Menegar/ManagerMenegarTeacherInClassView");
            if (!isValid)
                return Forbid();

            TheClass? theClass = await _context.TheClasses.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == Id && c.IdSchool == school && !c.IsDeleted);
            if (theClass == null)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));
                return View();
            }
            ViewBag.name = theClass?.Name ?? "Null";
            ViewBag.IdClass = idClass.ToString("D");
            return View();
        }

        [HttpGet]
        public JsonResult GetStudentCountPerClass()
        {
            Console.WriteLine("---------------------------------------");
            var data = _context.TheClasses.Where(c => /*c.IdSchool == HttpContext.Session.GetGuid("School") &&*/ c.IsDeleted == false )
                .Select(c => new {
                    ClassName = c.Name,
                    StudentCount = c.Students.Where(sc => sc.IsDeletedStudent == false).Count()
                }).ToList();
                Console.WriteLine("---------------------------------------");
                Console.WriteLine($"Count: {data.Count()}");

            return Json(data);
        }

        [HttpGet]
        public JsonResult GetTeacherCountPerSubject()
        {
            Console.WriteLine("---------------------------------------123");
            var data = _context.TeacherLectuerClasses.Where(t => t.IdSchool == HttpContext.Session.GetGuid("School") )
                .Include(t => t.IdLectuerNavigation) // تأكد من تضمين المادة
                .GroupBy(t => t.IdLectuerNavigation.Name)
                .Select(g => new
                {
                    subject = g.Key,
                    teacherCount = g.Where(x => x.IsDeletedTeacher == false).Select(x => x.IdTeacher).Distinct().Count()
                })
                .ToList();
            Console.WriteLine($"CountTeacher: {data.Count()}");
                
            return Json(data);
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        private bool MenegarExists(Guid id)
        {
            return _context.Menegars.Any(e => e.Id == id);
        }

        private void errorOperation(string messageNotyf, string source, Exception e)
        {
            _notyf.Error(messageNotyf);
            _logger.LogAsync(e, source);
        }

    }
}
