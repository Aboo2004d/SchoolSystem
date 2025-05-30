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
        

        public LectuerController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
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
                var totalRecords = await _context.Lectuers
                .Where(std => std.IdSchool == idSchool)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.Lectuers.Where(std => std.IdSchool == idSchool )
                    .AsNoTracking()
                    .Select(s => new
                    {
                        s.Id,
                        s.Name,
                        NumberOfStudentsInLectuer = s.StudentLectuerTeachers.Select(sc => sc.IdStudent).Distinct().Count(),
                        NumberOfTeacherInLectuer= s.TeacherLectuerClasses.Select(sc => sc.IdTeacher).Distinct().Count(),
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        s.Name.Contains(searchValue)
                    );
                }

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

                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

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
                Console.WriteLine($"Count Lectuers: {lectuers.Count()}");
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
                lectuer.IdSchool = HttpContext.Session.GetInt32("School") ?? 0;
                _context.Add(lectuer);
                await _context.SaveChangesAsync();
                _notyf.Success($"تمت عملية الاضافة بنجاح");
                return RedirectToAction(nameof(LectuerView));
            }
            return View(lectuer);
        }

        
        /*public IActionResult CreateStudentLectuer(int idLectuer)
        {
            var nameLectuer = _context.Lectuers.Where(lec => lec.Id == idLectuer).FirstOrDefault();
            if (nameLectuer == null)
            {
                return NotFound();
            }
            ViewBag.NameLectuer = nameLectuer.Name;
            ViewBag.IdLectuer = idLectuer;
            int id = HttpContext.Session.GetInt32("Id")??0;
            if (id == 0)
            {
                return NotFound();
            }
            var IdMenegar = _context.Menegars.Where(m => m.Id == id).FirstOrDefault();
            if (IdMenegar == null)
            {
                return NotFound();
            }
            var studentIdsInLectuer = _context.StudentLectuerTeachers
                .Where(sl => sl.IdLectuer == idLectuer)
                .Select(sl => sl.IdStudent)
                .ToList();

            var StudentInLectuer1 = _context.Students
                .Where(s => s.IdSchool == IdMenegar.IdSchool)
                .Where(s => !studentIdsInLectuer.Contains(s.Id))
                .ToList();
            
            ViewData["IdStudent"] = new SelectList(StudentInLectuer1, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudentLectuer([Bind("IdStudent,IdLectuer")] LectuerStudentViewModel studentLectuer)
        {

            // التحقق من صحة البيانات
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error}");
                }

                // عند وجود خطأ، يتم إعادة التوجيه إلى نفس الصفحة مع تمرير البيانات
                return RedirectToAction(nameof(CreateStudentLectuer), new { idLectuer = studentLectuer.IdLectuer });
            }

            try
            {
                
                var studentlectuer = _context.StudentLectuerTeachers
                .FirstOrDefault(sl => sl.IdStudent == studentLectuer.IdStudent && sl.IdLectuer == studentLectuer.IdLectuer);
                
                if(studentlectuer == null){
                    
                    var student = new StudentLectuerTeacher{
                        IdLectuer = studentLectuer.IdLectuer,
                        IdStudent = studentLectuer.IdStudent
                    };
                    
                    _context.StudentLectuerTeachers.Add(student);
                
                    int? studentclass = _context.StudentLectuerTeachers.Where(sc => sc.IdStudent == studentLectuer.IdStudent)
                    .Select(sc => (int?)sc.IdClass).FirstOrDefault();
                    if(studentclass != null){

                        int? teacherlectuer = _context.StudentLectuerTeachers.Where(tlc => tlc.IdLectuer == studentLectuer.IdLectuer)
                        .Select(t => (int?)t.IdTeacher).FirstOrDefault();

                        if(teacherlectuer!=null){

                            int? teacherlectuerclass = _context.TeacherLectuerClasses.Where(tc => tc.IdClass == studentclass )
                            .Select(t => (int?)t.IdTeacher).FirstOrDefault();
                            if(teacherlectuerclass != null){

                                var teachstu = new StudentLectuerTeacher{
                                    IdStudent = studentLectuer.IdStudent,
                                    IdTeacher = teacherlectuerclass??0
                                };

                                _context.StudentLectuerTeachers.Add(teachstu);
                                await _context.SaveChangesAsync();
                                return RedirectToAction("ManagerLectuer", new { idLectuer = studentLectuer.IdLectuer });

                            }else return RedirectToAction(nameof(CreateStudentLectuer), new { idLectuer = studentLectuer.IdLectuer });
                        }else return RedirectToAction(nameof(CreateStudentLectuer), new { idLectuer = studentLectuer.IdLectuer });
                    }else return RedirectToAction(nameof(CreateStudentLectuer), new { idLectuer = studentLectuer.IdLectuer });
                }else return RedirectToAction(nameof(CreateStudentLectuer), new { idLectuer = studentLectuer.IdLectuer });
                
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                ModelState.AddModelError("", "حدث خطأ أثناء حفظ البيانات. الرجاء المحاولة مرة أخرى.");
                // إعادة المستخدم إلى نفس الصفحة مع البيانات الحالية
                return RedirectToAction(nameof(CreateStudentLectuer), new { idLectuer = studentLectuer.IdLectuer });
            }
        }
*/
        [HttpGet]
        public IActionResult CreateTeacherLectuer(int idLectuer)
        {
            Exception ex = new Exception();
            Lectuer? nameLectuer = _context.Lectuers.Where(lec => lec.Id == idLectuer).FirstOrDefault();
            if (nameLectuer == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                ex = new Exception("تلاعب بالبيانات المرسلة");
                _logger.LogAsync(ex, "Lectuer/CreateTeacherLectuer");
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
        public async Task<IActionResult> CreateTeacherInLectuer(int idLectuer,[Bind("IdTeacher,IdLectuer")] LectuerTeacherViewModel teacherLectuer)
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
                                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                                    await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "Lectuer/CreateTeacherLectuer");
                                }
                            }
                            else
                            {
                                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                                await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "Lectuer/CreateTeacherLectuer");
                            }
                        }
                        else
                        {
                            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                            await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "Lectuer/CreateTeacherLectuer");
                        }
                    }
                    else
                    {
                        _notyf.Error("البيانات المرسلة خاطئة");
                    }
                }
                else
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "Lectuer/CreateTeacherLectuer");
                }

            }
            catch (Exception ex)
            {
                _notyf.Error("حدث خطا غير متوقع\nحاول لاحقا");
                await _logger.LogAsync(ex, "Lectuer/CreateTeacherLectuer");
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
                e = new Exception("البيانات المرسلة غير صحيحة");
                await _logger.LogAsync(e, "TheClass/Edit");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ");
                return RedirectToAction(nameof(LectuerView));
            }

            if (ModelState.IsValid)
            {
                try
                {
                    Lectuer lect = await _context.Lectuers.FirstOrDefaultAsync(c => c.Id == lectuer.Id);
                    if (lect == null)
                    {
                        e = new Exception("تلاعب بالبيانات المرسلة للتحقق و الحفظ");
                        await _logger.LogAsync(e, "TheClass/Edit");
                        _notyf.Error(" لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ");
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
                    await _logger.LogAsync(ex, "TheClass/Edit");
                    _notyf.Error("حدث خطأ غير متوقع\nحاول مرة اخرى لاحقا");
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
                    .Where(std => std.IdSchool == idSchool && std.IdLectuer == idLectuer)
                    .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.TeacherLectuerClasses.Where(std => std.IdSchool == idSchool && std.IdLectuer == idLectuer)
                    .Include(s => s.IdClassNavigation)
                    .Include(s => s.IdTeacherNavigation)
                    .Include(s => s.IdLectuerNavigation)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        ClassroomName = s.IdClassNavigation!= null?s.IdClassNavigation.Name:"UnKnown",
                        TeacherName = s.IdTeacherNavigation!= null?s.IdTeacherNavigation.Name:"UnKnown",
                        IdTeacher = s.IdTeacher,
                        LectureName = s.IdLectuerNavigation!= null?s.IdLectuerNavigation.Name:"UnKnown",
                        IdLectuer = s.IdLectuer,
                        
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        s.TeacherName.Contains(searchValue)||
                        s.ClassroomName.Contains(searchValue)||
                        s.LectureName.Contains(searchValue)
                    );
                }

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
                var data = await query
                            .Skip(start)
                            .Take(length)
                            .ToListAsync(); // بدون Select هنا

                // التقطيع (Pagination)
                var teachersLectuer = data.
                Select(s => new LectuerViewModel {
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
                Console.WriteLine($"Count Lectuers: {teachersLectuer.Count()}");
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
        public IActionResult TeacherLectuerView(int idLectuer)
        {
            ViewBag.IdLect = idLectuer;
            var name = _context.Lectuers.FirstOrDefault(c => c.Id == idLectuer);
            ViewBag.name = name?.Name??"Null";
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
                var totalRecords = await _context.StudentLectuerTeachers
                .Where(std => std.IdSchool == idSchool && std.IdLectuer == idLectuer)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.StudentLectuerTeachers.Where(std => std.IdSchool == idSchool && std.IdLectuer == idLectuer)
                    .Include(s => s.IdClassNavigation)
                    .Include(s => s.IdStudentNavigation)
                    .Include(s => s.IdClassNavigation)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        StudentName = s.IdStudentNavigation!= null?s.IdStudentNavigation.Name:"UnKnown",
                        IdStudent = s.IdStudent,
                        ClassroomName = s.IdClassNavigation!= null?s.IdClassNavigation.Name:"UnKnown",
                        TeacherName = s.IdTeacherNavigation!= null?s.IdTeacherNavigation.Name:"UnKnown",
                        IdTeacher = s.IdTeacher,
                        LectureName = s.IdLectuerNavigation!= null?s.IdLectuerNavigation.Name:"UnKnown",
                        IdLectuer = s.IdLectuer,
                        
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        s.StudentName.Contains(searchValue)||
                        s.TeacherName.Contains(searchValue)||
                        s.ClassroomName.Contains(searchValue)||
                        s.LectureName.Contains(searchValue)
                    );
                }

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

                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

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
                Console.WriteLine($"Count Lectuers: {studentsLectuer.Count()}");
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
        public IActionResult StudentLectuerView(int idLectuer)
        {
            ViewBag.IdLect = idLectuer;
            var name = _context.Lectuers.FirstOrDefault(c => c.Id == idLectuer);
            ViewBag.name = name?.Name??"Null";
            ViewBag.IdLectuer = Request.Query["idLectuer"];
            return View();
        }


        [AuthorizeRoles("admin")]
        private bool LectuerExists(int id)
        {
            return _context.Lectuers.Any(e => e.Id == id);
        }
    }
}
