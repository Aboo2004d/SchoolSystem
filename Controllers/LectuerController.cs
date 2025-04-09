using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public LectuerController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: Lectuer
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> Index()
        {
            var students = await _context.Lectuers
                .Include(sl => sl.StudentLectuers)
                .ThenInclude(sl => sl.IdStudentNavigation)
                .Select(ts => new Lectuer{
                    NumberOfStudentsInLectuer = ts.StudentLectuers.Select(sc => sc.IdStudent).Distinct().Count(),
                    NumberOfTeacherInLectuer= ts.TeacherLectuers.Select(sc => sc.IdTeacher).Distinct().Count(),
                    Name = ts.Name,
                    Id = ts.Id
                    })
                    .ToListAsync();
            return View(students);
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
        public async Task<IActionResult> Create([Bind("Id,Name")] Lectuer lectuer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(lectuer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(lectuer);
        }

        
        public IActionResult CreateStudentLectuer(int idLectuer)
        {
            ViewBag.Lec = new SelectList(_context.Lectuers, "Id", "Name",idLectuer);
            ViewBag.IdLectuer = idLectuer;
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudentLectuer([Bind("IdStudent,IdLectuer")] LectuerStudentViewModel studentLectuer)
        {
            Console.WriteLine($"Id");

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
                //البحث عن الطالب اذا كان مسجل المادة قبل هيك
                var studentlectuer = _context.StudentLectuers
                .FirstOrDefault(sl => sl.IdStudent == studentLectuer.IdStudent && sl.IdLectuer == studentLectuer.IdLectuer);
                if(studentlectuer == null){//اذا فارغ يعني ما سجل المادة قبل هيك وبالتالي يسمح له بتسجيلها
                    var student = new StudentLectuer{
                        IdLectuer = studentLectuer.IdLectuer,
                        IdStudent = studentLectuer.IdStudent
                    };//اضافة البيانات الى اقاعدة البيانات 
                    _context.StudentLectuers.Add(student);
                    //العثور على الصف المسجل به الطالب
                    var studentclass = _context.StudentClasses.Where(sc => sc.IdStudent == studentLectuer.IdStudent)
                    .Select(sc => sc.IdClass).FirstOrDefault();
                    if(studentclass != null){//اذا مش فارغ يعني الطالب موجود في صف
                        var teacherlectuer = _context.TeacherLectuers.Where(tl => tl.IdLectuer == studentLectuer.IdLectuer)
                        .Select(t => t.IdTeacher).FirstOrDefault();//الحصول على معلمين المادة
                        if(teacherlectuer!=null){
                            //الحصول على المعلم المسجل في صف الطالب 
                            var teacherlectuerclass = _context.TeacherClasses.Where(tc => tc.IdClass == studentclass )
                            .Select(t => t.IdTeacher).FirstOrDefault();
                            Console.WriteLine($"Teacher: {teacherlectuerclass}");
                            if(teacherlectuerclass != null){ //اذا مش فارغ يعني المعلم موجود في صف الطالب ويدرس نفس مادة الطالب
                                //بالتالي الطالب موجود في صف وقد حصلنا على المعلم الذي يدرس المادة لنفس صف الطالب
                                var teachstu = new StudentTeacher{
                                    IdStudent = studentLectuer.IdStudent,
                                    IdTeacher = teacherlectuerclass
                                };
                                _context.StudentTeachers.Add(teachstu);
                                await _context.SaveChangesAsync();
                                return RedirectToAction("ManagerLectuer", new { idLectuer = studentLectuer.IdLectuer });

                            }else Console.WriteLine(4);
                        }else Console.WriteLine(3);
                    }else Console.WriteLine(2);
                }else Console.WriteLine(1);
                    
                // الحصول على الرابط السابق من الهيدر
                return RedirectToAction(nameof(CreateStudentLectuer), new { idLectuer = studentLectuer.IdLectuer });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                ModelState.AddModelError("", "حدث خطأ أثناء حفظ البيانات. الرجاء المحاولة مرة أخرى.");
                // إعادة المستخدم إلى نفس الصفحة مع البيانات الحالية
                return RedirectToAction(nameof(CreateStudentLectuer), new { idLectuer = studentLectuer.IdLectuer });
            }
        }

        public IActionResult CreateTeacherLectuer(int idLectuer)
        {
            
            ViewBag.Lec = new SelectList(_context.Lectuers, "Id", "Name",idLectuer);
            ViewBag.IdLectuer = idLectuer;
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name");
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Name");

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacherInLectuer([Bind("IdTeacher,IdLectuer,IdClass")] LectuerTeacherViewModel teacherLectuer)
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
                return RedirectToAction(nameof(CreateTeacherLectuer), new { idLectuer = teacherLectuer.IdLectuer });
            }

            try
            {
                var teachers = _context.TeacherLectuers.Where(tl => tl.IdLectuer == teacherLectuer.IdLectuer).Select(t => t.IdTeacher).FirstOrDefault();
                var teachersclass = _context.TeacherClasses.Where(tc => tc.IdTeacher == teachers).Select(t => t.IdClass);
                if(teachersclass ==null){
                    var classrom = _context.TheClasses.Where(c => c.Id == teacherLectuer.IdClass).FirstOrDefault();
                    var teacherlectuer = new TeacherLectuer{
                        IdLectuer = teacherLectuer.IdLectuer,
                        IdTeacher = teacherLectuer.IdTeacher
                    };
                    // إضافة البيانات إلى قاعدة البيانات
                    _context.TeacherLectuers.Add(teacherlectuer);
                    await _context.SaveChangesAsync();
                    var teacherclass = new TeacherClass{
                        IdClass = teacherLectuer.IdClass,
                        IdTeacher = teacherLectuer.IdTeacher
                    };
                     _context.TeacherClasses.Add(teacherclass);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ManagerTeacherLectuer", new { idLectuer = teacherLectuer.IdLectuer });
                }
                
                
                return RedirectToAction(nameof(CreateTeacherLectuer), new { idLectuer = teacherLectuer.IdLectuer });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                ModelState.AddModelError("", "حدث خطأ أثناء حفظ البيانات. الرجاء المحاولة مرة أخرى.");

                // إعادة المستخدم إلى نفس الصفحة مع البيانات الحالية
                return RedirectToAction(nameof(CreateTeacherLectuer), new { idLectuer = teacherLectuer.IdLectuer });
            }
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
            if (id != lectuer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lectuer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LectuerExists(lectuer.Id))
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
            var lectuer = await _context.Lectuers.FindAsync(id);
            if (lectuer != null)
            {
                var StudentInLectuer = _context.StudentLectuers
                .Where(sl => sl.IdLectuer == id);
                var TeacherInLectuer = _context.TeacherLectuers
                .Where(sl => sl.IdLectuer == id);
                var ClassInLectuer = _context.ClassLectuers
                .Where(sl => sl.IdLectuer == id);
                foreach (var item in StudentInLectuer)
                {
                    _context.StudentLectuers.Remove(item);
                }
                foreach (var item in TeacherInLectuer)
                {
                    _context.TeacherLectuers.Remove(item);
                }
                foreach (var item in ClassInLectuer)
                {
                    _context.ClassLectuers.Remove(item);
                }
                _context.Lectuers.Remove(lectuer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerLectuer([FromQuery]int idLectuer)
        {
            try{
                var studentlecturer = await _context.StudentLectuers
                .Where(sl => sl.IdLectuer == idLectuer)
                .Include(sl => sl.IdStudentNavigation) // تحميل بيانات الطالب
                .Include(sl => sl.IdStudentNavigation.StudentClasses) // تحميل الفصول المرتبطة بالطالب
                    .ThenInclude(sc => sc.IdClassNavigation) // تحميل بيانات الفصل الدراسي
                        .ThenInclude(c => c.TeacherClasses) // تحميل المعلمين المرتبطين بالفصل
                            .ThenInclude(tc => tc.IdTeacherNavigation) // تحميل بيانات المعلم
                .Include(sl => sl.IdLectuerNavigation) // تحميل بيانات المحاضرة
                .Select(sl => new LectuerViewModel
                {
                    Id = sl.IdStudentNavigation.Id,
                    StudentName = sl.IdStudentNavigation.Name,
                    ClassroomName = sl.IdStudentNavigation.StudentClasses
                        .Select(sc => sc.IdClassNavigation.Name)
                        .FirstOrDefault(),
                    TeacherName = sl.IdStudentNavigation.StudentClasses
                        .SelectMany(sc => sc.IdClassNavigation.TeacherClasses)
                        .Select(tc => tc.IdTeacherNavigation.Name)
                        .FirstOrDefault(),
                    IdLectuer = idLectuer,
                    LectureName = sl.IdLectuerNavigation.Name
                })
                .ToListAsync();

                foreach(var i in studentlecturer){
                    Console.WriteLine($"Class Name: {i.ClassroomName}");
                    Console.WriteLine($"Teacher: {i.TeacherName}");
                }
                if (!studentlecturer.Any())
                {
                    studentlecturer.Add(new LectuerViewModel
                    {
                        IdLectuer = idLectuer
                    });
                } 
                return View(studentlecturer);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerTeacherLectuer([FromQuery]int idLectuer)
        {
            try{
                var teacher = _context.TeacherLectuers.Where(tl => tl.IdLectuer == idLectuer).ToList();
                var teacherlectuer = _context.TeacherLectuers
                .Where(tl => tl.IdLectuer == idLectuer)
                .SelectMany(tl => _context.TeacherClasses
                .Where(tc => tc.IdTeacher == tl.IdTeacher)
                .Select(tlc => new LectuerViewModel{
                    Id = tl.Id,
                    TeacherName = tl.IdTeacherNavigation.Name,
                    ClassroomName = tlc.IdClassNavigation.Name,
                    IdLectuer = tl.IdLectuerNavigation.Id,
                    LectureName = tl.IdLectuerNavigation.Name
                })
                ).ToList();
                    
                    foreach(var i in teacherlectuer){
                        Console.WriteLine($"Class Name: {i.ClassroomName}");
                        Console.WriteLine($"Teacher: {i.TeacherName}");
                    }
                    if (!teacherlectuer.Any())
                    {
                        teacherlectuer.Add(new LectuerViewModel
                        {
                            IdLectuer = idLectuer
                        });
                } 
                return View(teacherlectuer);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }


        [AuthorizeRoles("admin")]
        private bool LectuerExists(int id)
        {
            return _context.Lectuers.Any(e => e.Id == id);
        }
    }
}
