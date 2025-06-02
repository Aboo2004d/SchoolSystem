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

namespace SchoolSystem.Controllers
{
    public class LectuerController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;


        public LectuerController(SystemSchoolDbContext context, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
        }

        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Lectuers(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, status) = await _sessionValidatorService.ValidateAdminSessionAsync(HttpContext, "Lectuer/Lectuers");
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
                var totalRecords = await _context.Lectuers
                .Where(std => std.IdSchool == IdSchool)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Lectuers.Where(std => std.IdSchool == IdSchool)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        NumberOfStudentsInLectuer = s.StudentLectuerTeachers.Select(sc => sc.IdStudent).Distinct().Count(),
                        NumberOfTeacherInLectuer = s.TeacherLectuerClasses.Select(sc => sc.IdTeacher).Distinct().Count(),
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        s.Name.Contains(searchValue)
                    );
                }

                // الحصول على القيم بعد الفلترة
                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.Name),
                    ("0", "desc") => query.OrderByDescending(s => s.Name),
                    ("1", "asc") => query.OrderBy(s => s.NumberOfStudentsInLectuer),
                    ("1", "desc") => query.OrderByDescending(s => s.NumberOfStudentsInLectuer),
                    ("2", "asc") => query.OrderBy(s => s.NumberOfTeacherInLectuer),
                    ("2", "desc") => query.OrderByDescending(s => s.NumberOfTeacherInLectuer),
                    _ => query.OrderBy(s => s.Name)
                };

                // التقطيع (Pagination)
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // الحصول على البيانات للعرض
                var lectuers = data.
                Select(s => new Lectuer
                {
                    Id = s.Id,
                    Name = s.Name,
                    NumberOfStudentsInLectuer = s.NumberOfStudentsInLectuer,
                    NumberOfTeacherInLectuer = s.NumberOfTeacherInLectuer
                })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = lectuers
                };
                Console.WriteLine($"Count: {totalRecords}");

                return Json(result);
            }
            catch (Exception e)
            {
                await _logger.LogAsync(e, "Lectuer/Lectuers");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("admin")]
        public IActionResult LectuerView()
        {
            return View();
        }

        // GET: Lectuer/Details/5
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lectuer = await _context.Lectuers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lectuer == null)
            {
                return NotFound();
            }

            return View(lectuer);
        }

        // GET: Lectuer/Create
        [AuthorizeRoles("admin")]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Lectuer/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Create([Bind("Name")] Lectuer lectuer)
        {
            if (ModelState.IsValid)
            {
                if (string.IsNullOrEmpty(lectuer.Name))
                {
                    _notyf.Error("الرجاء ادخال اسم المادة");
                    return View(lectuer);
                }
                int school = HttpContext.Session.GetInt32("School") ?? 0;
                if (_context.Lectuers.Any(c => c.Name == lectuer.Name && c.IdSchool == school))
                {
                    _notyf.Error("المادة موجودة مسبقا");
                    return View(lectuer);
                }
                lectuer.IdSchool = school;
                _context.Add(lectuer);
                await _context.SaveChangesAsync();
                _notyf.Success($"تمت عملية الاضافة بنجاح");
                return RedirectToAction(nameof(LectuerView));
            }
            return View(lectuer);
        }

        [HttpGet]
        public IActionResult CreateTeacherLectuer(int idLectuer)
        {
            Exception ex = new Exception();
            Lectuer? nameLectuer = _context.Lectuers.Where(lec => lec.Id == idLectuer).FirstOrDefault();
            if (nameLectuer == null)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));
                return View(nameof(LectuerView));
            }
            ViewBag.NameLectuer = nameLectuer.Name;
            ViewBag.IdLectuer = idLectuer;
            ViewData["IdTeacher"] = new SelectList(_context.Teachers
            .Where(s => s.IdSchool == HttpContext.Session.GetInt32("School")), "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacherInLectuer(int idLectuer, [Bind("IdTeacher,IdLectuer")] LectuerTeacherViewModel teacherLectuer)
        {
            try
            {
                if (idLectuer == teacherLectuer.IdLectuer)
                {

                    if (ModelState.IsValid)
                    {
                        Teacher? teacher = _context.Teachers.FirstOrDefault(t => t.Id == teacherLectuer.IdTeacher);
                        if (teacher != null)
                        {
                            Lectuer? lectuer = _context.Lectuers.FirstOrDefault(t => t.Id == teacherLectuer.IdLectuer);
                            if (lectuer != null)
                            {
                                if (HttpContext.Session.GetInt32("School") != 0)
                                {
                                    TeacherLectuerClass teacherclass = new TeacherLectuerClass
                                    {
                                        IdTeacher = teacherLectuer.IdTeacher,
                                        IdLectuer = teacherLectuer.IdLectuer,
                                        IdSchool = HttpContext.Session.GetInt32("School") ?? 0
                                    };
                                    _context.TeacherLectuerClasses.Add(teacherclass);
                                    await _context.SaveChangesAsync();
                                    _notyf.Success($"تمت اضافة المعلم لتدريس المادة");
                                    return RedirectToAction("TeacherLectuerView", new { idLectuer = teacherLectuer.IdLectuer });
                                }
                                else
                                {
                                    errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));
                                }
                            }
                            else
                            {
                                errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));
                                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                                await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "Lectuer/CreateTeacherLectuer");
                            }
                        }
                        else
                        {
                            errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));

                        }
                    }
                    else
                    {
                        _notyf.Error("البيانات المرسلة خاطئة");
                    }
                }
                else
                {
                    errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));

                }

            }
            catch (Exception ex)
            {
                errorOperation("حدث خطأ غير متوقع\nحاول مرة اخرى لاحقا", "Lectuer/CreateTeacherLectuer", ex );
            }
            return RedirectToAction("TeacherLectuerView", new { idLectuer = teacherLectuer.IdLectuer });
        }


        // GET: Lectuer/Edit/5
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lectuer = await _context.Lectuers.FindAsync(id);
            if (lectuer == null)
            {
                return NotFound();
            }
            return View(lectuer);
        }

        // POST: Lectuer/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] Lectuer lectuer)
        {
            Exception e = new Exception();
            if (id != lectuer.Id)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ", "Lectuer/Edit", new Exception("البيانات المرسلة غير صحيحة"));
                return RedirectToAction(nameof(LectuerView));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Lectuer? lect = await _context.Lectuers.FirstOrDefaultAsync(c => c.Id == lectuer.Id);
                    if (lect == null)
                    {
                        errorOperation("لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ", "Lectuer/Edit", new Exception("تلاعب بالبيانات المرسلة للتحقق و الحفظ"));
                       return RedirectToAction(nameof(LectuerView));
                    }
                    if (lect.Name == lectuer.Name)
                    {
                        _notyf.Error("المادة موجودة مسبقا");
                        return View(lectuer);
                    }
                    lect.Name = lectuer.Name;
                    await _context.SaveChangesAsync();
                    _notyf.Success("تمت عملية التعديل بنجاح");
                    return RedirectToAction(nameof(LectuerView));
                }
                catch (Exception ex)
                {
                    errorOperation("حدث خطأ غير متوقع\nحاول مرة اخرى لاحقا", "Lectuer/Edit", ex );
                    return View(lectuer);
                }

            }
            _notyf.Error("البيانات المدخلة خاطئة");
            return View(lectuer);
        }

        // GET: Lectuer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lectuer = await _context.Lectuers
                .FirstOrDefaultAsync(m => m.Id == id);
            if (lectuer == null)
            {
                return NotFound();
            }

            return View(lectuer);
        }

        // POST: Lectuer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Console.WriteLine($"DeleteConfirmed: {id}");
            var lectuer = await _context.Lectuers.FindAsync(id);
            if (lectuer != null)
            {
                _context.Lectuers.Remove(lectuer);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }


        [AuthorizeRoles("admin")]
        public async Task<IActionResult> TeacherLectuer(
            int idLectuer,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, status) = await _sessionValidatorService.ValidateAdminSessionAsync(HttpContext, "Lectuer/TeacherLectuer");
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
                var totalRecords = await _context.TeacherLectuerClasses
                    .Where(std => std.IdSchool == IdSchool && std.IdLectuer == idLectuer)
                    .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.TeacherLectuerClasses.Where(std => std.IdSchool == IdSchool && std.IdLectuer == idLectuer)
                    .Include(s => s.IdClassNavigation)
                    .Include(s => s.IdTeacherNavigation)
                    .Include(s => s.IdLectuerNavigation)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "UnKnown",
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "UnKnown",
                        IdTeacher = s.IdTeacher,
                        LectureName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "UnKnown",
                        IdLectuer = s.IdLectuer,


                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.TeacherName != null && s.TeacherName.Contains(searchValue)) ||
                        (s.TeacherName != null && s.ClassroomName.Contains(searchValue)) ||
                        (s.TeacherName != null && s.LectureName.Contains(searchValue))
                    );
                }
                // الحصول على القيم بعد الفلترة
                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("0", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    ("1", "asc") => query.OrderBy(s => s.TeacherName),
                    ("1", "desc") => query.OrderByDescending(s => s.TeacherName),
                    ("2", "asc") => query.OrderBy(s => s.LectureName),
                    ("2", "desc") => query.OrderByDescending(s => s.LectureName),
                    _ => query.OrderBy(s => s.TeacherName)
                };

                // التقطيع (Pagination)
                var data = await query
                            .Skip(start)
                            .Take(length)
                            .ToListAsync();

                // ارسال البيانات الى العرض
                var teachersLectuer = data.
                Select(s => new LectuerViewModel
                {
                    Id = s.Id,
                    LectureName = s.LectureName,
                    IdLectuer = s.IdLectuer,
                    TeacherName = s.TeacherName,
                    IdTeacher = s.IdTeacher,
                    ClassroomName = s.ClassroomName
                }).ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = teachersLectuer
                };
                
                return Json(result);
            }
            catch (Exception e)
            {
                await _logger.LogAsync(e, "Lectuer/Lectuers");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> TeacherLectuerView(int idLectuer)
        {
            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdSchool, status) = await _sessionValidatorService.ValidateAdminSessionAsync(HttpContext, "Lectuer/TeacherLectuer");
            if (!IsValid)
            {
                if (!status)
                    return RedirectToAction("Login", "Account");

                return View(nameof(LectuerView));
            }
            Lectuer? lectuer = await _context.Lectuers.SingleOrDefaultAsync(c => c.Id == idLectuer);
            if (lectuer == null)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));
                return View(nameof(LectuerView));
            
            }
            ViewBag.name = lectuer?.Name ?? "Null";
            ViewBag.IdLectuer = Request.Query["idLectuer"];
            return View();
        }

        [AuthorizeRoles("admin")]
        public async Task<IActionResult> StudentLectuer(
            int idLectuer,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, status) = await _sessionValidatorService.ValidateAdminSessionAsync(HttpContext, "Lectuer/TeacherLectuer");
                if (!IsValid)
                {
                    return Json(new { success = false, status = status, error = "Unauthorized access. Session expired." });
                }

                if (length <= 0)
                    length = 10;

                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.StudentLectuerTeachers
                .Where(std => std.IdSchool == IdSchool && std.IdLectuer == idLectuer)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.StudentLectuerTeachers.Where(std => std.IdSchool == IdSchool && std.IdLectuer == idLectuer)
                    .Include(s => s.IdClassNavigation)
                    .Include(s => s.IdStudentNavigation)
                    .Include(s => s.IdClassNavigation)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        StudentName = s.IdStudentNavigation != null ? s.IdStudentNavigation.Name : "UnKnown",
                        IdStudent = s.IdStudent,
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "UnKnown",
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "UnKnown",
                        IdTeacher = s.IdTeacher,
                        LectureName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "UnKnown",
                        IdLectuer = s.IdLectuer,


                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.StudentName != null && s.StudentName.Contains(searchValue)) ||
                        (s.TeacherName != null && s.TeacherName.Contains(searchValue)) ||
                        (s.ClassroomName != null && s.ClassroomName.Contains(searchValue)) ||
                        (s.LectureName != null && s.LectureName.Contains(searchValue))
                    );
                }

                // الحصول على القيم بعد الفلترة
                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.StudentName),
                    ("0", "desc") => query.OrderByDescending(s => s.StudentName),
                    ("1", "asc") => query.OrderBy(s => s.TeacherName),
                    ("1", "desc") => query.OrderByDescending(s => s.TeacherName),
                    ("2", "asc") => query.OrderBy(s => s.LectureName),
                    ("2", "desc") => query.OrderByDescending(s => s.LectureName),
                    ("3", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("3", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    _ => query.OrderBy(s => s.StudentName)
                };

                // تقطيع
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // الحصول على البيانات للعرض
                var studentsLectuer = data.
                Select(s => new LectuerViewModel
                {
                    Id = s.Id,
                    LectureName = s.LectureName,
                    IdLectuer = s.IdLectuer,
                    TeacherName = s.TeacherName,
                    IdTeacher = s.IdTeacher,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName
                })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = studentsLectuer
                };
                
                return Json(result);
            }
            catch (Exception e)
            {
                await _logger.LogAsync(e, "Lectuer/Lectuers");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return Json(new { error = e.Message, stack = e.StackTrace });
            }
        }


        [HttpGet]
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> StudentLectuerView(int idLectuer)
        {
            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdSchool, status) = await _sessionValidatorService.ValidateAdminSessionAsync(HttpContext, "Lectuer/TeacherLectuer");
            if (!IsValid)
            {
                if (!status)
                    return RedirectToAction("Login", "Account");
                return View(nameof(LectuerView));
            }
            Lectuer? lectuer = await _context.Lectuers.SingleOrDefaultAsync(c => c.Id == idLectuer);
            if (lectuer == null)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));
                return View(nameof(LectuerView));
            
            }
            ViewBag.name = lectuer?.Name ?? "Null";
            ViewBag.IdLectuer = Request.Query["idLectuer"];
            return View();
        }


        [AuthorizeRoles("admin")]
        private bool LectuerExists(int id)
        {
            return _context.Lectuers.Any(e => e.Id == id);
        }

        private void errorOperation(string messageNotyf, string source, Exception e)
        {
            _notyf.Error(messageNotyf);
            _logger.LogAsync(e, source);
        }

    }
}
