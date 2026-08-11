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

        

        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public IActionResult LectuerView()
        {
            return View();
        }

        // GET: Lectuer/Details/5
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> Details(Guid? id)
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        // POST: Lectuer/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        
        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> CreateTeacherLectuer(Guid idLectuer)
        {
            if (idLectuer == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "Lectuer/CreateTeacherLectuer");
                return NotFound();
            }
            ViewBag.IdLectuer = idLectuer;
            return View();
        }
        
        // GET: Lectuer/Edit/5
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public  IActionResult Edit(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            ViewBag.Id = id ?? Guid.Empty;

            return View();
        }

        // POST: Lectuer/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> Edit(LectuerDataViewModel lectuer)
        {
            if (lectuer.Id == null)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ", "Lectuer/Edit", new Exception("البيانات المرسلة غير صحيحة"));
                return RedirectToAction(nameof(Edit));
            }

            Guid Id;

            try
            {
                Id = lectuer.Id;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Student/Delete");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarClassView","Menegar");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Lectuer? lect = await _context.Lectuers.FirstOrDefaultAsync(c => c.Id == Id);
                    if (lect == null)
                    {
                        errorOperation("لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ", "Lectuer/Edit", new Exception("تلاعب بالبيانات المرسلة للتحقق و الحفظ"));
                       return RedirectToAction(nameof(LectuerView));
                    }
                    if (lect.Name == lectuer.Name)
                    {
                        _notyf.Error("اسم المادة كما هو لم يتغير");
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
        public async Task<IActionResult> Delete(Guid? id)
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            Guid Id;

            try
            {
                Id = id;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Lectuer/Delete");
                _notyf.Error("حدث خطأ غير متوقع.");
                return View(nameof(LectuerView));
            }
            
            var lectuer = await _context.Lectuers.FindAsync(Id);

            if (lectuer != null)
            {
                lectuer.IsDeleted = true;

            }
            await _context.SaveChangesAsync();
            return View(nameof(LectuerView));
        }

        // GET: Lectuer/Delete/5
        public async Task<IActionResult> DeleteTeacherLectuer(Guid? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            Guid Id;

            try
            {
                Id = id.Value;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Lectuer/DeleteTeacherLectuer");
                _notyf.Error("حدث خطأ غير متوقع.");
                return View(nameof(TeacherLectuerView));
            }

            var lectuer = await _context.TeacherLectuerClasses
                .FirstOrDefaultAsync(m => m.Id == Id);

            if (lectuer == null)
            {
                return NotFound();
            }

            return View(lectuer);
        }

        // POST: Lectuer/Delete/5
        [HttpPost, ActionName("DeleteTeacherLectuer")]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> DeleteTeacherLectuerConfirmed(Guid id)
        {
            Guid Id;

            try
            {
                Id = id;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Delete");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("LectuerView");
            }
            
            var teacherLectuer = await _context.TeacherLectuerClasses.FindAsync(Id);

            if (teacherLectuer != null)
            {
                teacherLectuer.IsDeletedLectuer = true;
                _notyf.Success("تمت ازالة المعلم من المادة");
            }
            await _context.SaveChangesAsync();

            return RedirectToAction("TeacherLectuerView",new{idLectuer = teacherLectuer.IdLectuer ?? Guid.Empty});
        }


        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> TeacherLectuer(
            Guid idLectuer,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            Console.WriteLine("------------------------------------");
            Guid Id;
            Console.WriteLine($"IdLectuer: {idLectuer}");
            try
            {
                Id = idLectuer;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/TeacherLectuer");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarClassView","Menegar");
            }
            
            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Lectuer/TeacherLectuer");
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
                    .Where(std => std.IdSchool == IdSchool && std.IsDeletedTeacher == false && std.IdLectuer == Id)
                    .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.TeacherLectuerClasses.Where(std => std.IdSchool == IdSchool && std.IsDeletedTeacher == false && std.IdLectuer == Id)
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
                Select(s => new LectuerInTeacherViewModel
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> TeacherLectuerView(Guid idLectuer)
        {
            Console.WriteLine("------------------------------------");
            Guid Id;
            Console.WriteLine($"IdLectuer: {idLectuer}");

            try
            {
                Id = idLectuer;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Lectuer/TeacherLectuerView");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarClassView","Menegar");
            }

            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Lectuer/TeacherLectuer");
            if (!IsValid)
            {
                

                return View(nameof(LectuerView));
            }
            Lectuer? lectuer = await _context.Lectuers.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == Id && c.IdSchool == IdSchool && !c.IsDeleted);
            if (lectuer == null)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/CreateTeacherLectuer", new Exception("تلاعب بالبيانات المرسلة"));
                return View(nameof(LectuerView));
            
            }
            ViewBag.name = lectuer?.Name ?? "Null";
            ViewBag.IdLectuer = Request.Query["idLectuer"];
            return View();
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> StudentLectuer(
            Guid idLectuer,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            Console.WriteLine("------------------------------------");
            Guid Id;
            Console.WriteLine($"IdLectuer: {idLectuer}");
            try
            {
                Id = idLectuer;
                Console.WriteLine($"Id: {Id}");

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Lectuer/StudentLectuer");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarClassView","Menegar");
            }

            try
            {
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Lectuer/StudentLectuer");
                if (!IsValid)
                {
                    return Json(new { success = false, message = Message, error = "Unauthorized access. Session expired." });
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
                .Where(std => std.IdSchool == IdSchool && std.IdLectuer == Id)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.StudentLectuerTeachers.Where(std => std.IdSchool == IdSchool && std.IdLectuer == Id)
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
                        TeacherName = _context.TeacherLectuerClasses
                            .Where(t =>
                                t.IdTeacher == s.IdTeacher &&
                                t.IdLectuer == s.IdLectuer &&
                                t.IdClass == s.IdClass &&
                                t.IsDeletedTeacher == false
                            )
                            .Select(t => t.IdTeacherNavigation.Name)
                            .FirstOrDefault() ?? s.IdTeacherNavigation.Name + " (معلم مزال)",
                        
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
                Select(s => new LectuerInStudentViewModel
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
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> StudentLectuerView(Guid idLectuer)
        {

            Guid Id;

            try
            {
                Id = idLectuer;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Lectuer/StudentLectuerView");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("LectuerView");
            }

            // التحقق من صلاحية المستخدم و التلاعب بالبيانات
            var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Lectuer/TeacherLectuer");
            if (!IsValid)
            {
                
                return View(nameof(LectuerView));
            }
            Lectuer? lectuer = await _context.Lectuers.AsNoTracking()
                .SingleOrDefaultAsync(c => c.Id == Id && c.IdSchool == IdSchool && !c.IsDeleted);
            if (lectuer == null)
            {
                errorOperation("لا يمكن التلاعب بالبيانات المرسلة", "Lectuer/StudentLectuerView", new Exception("تلاعب بالبيانات المرسلة"));
                return View(nameof(LectuerView));
            
            }
            ViewBag.name = lectuer?.Name ?? "Null";
            ViewBag.IdLectuer = Request.Query["idLectuer"];
            return View();
        }


        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        private bool LectuerExists(Guid id)
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
