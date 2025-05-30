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
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    public class TheClassController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        

        public TheClassController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
        }

        // GET: TheClass
        public async Task<IActionResult> Index()
        {
            return View(await _context.TheClasses.ToListAsync());
        }

        // GET: TheClass/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theClass = await _context.TheClasses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (theClass == null)
            {
                return NotFound();
            }

            return View(theClass);
        }

        // GET: TheClass/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TheClass/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] TheClass theClass)
        {
            Exception ex = new Exception();
            if (ModelState.IsValid)
            {
                bool name = await _context.TheClasses.AnyAsync(nc => nc.Name == theClass.Name);
                if (name)
                {
                    _notyf.Error("الصف موجود مسبقا");
                    return View(theClass);
                }
                int school = HttpContext.Session.GetInt32("School") ?? 0;
                if (school == 0)
                {
                    ex = new Exception("Bypass verification system");
                    await _logger.LogAsync(ex, "TheClass/Create");
                    _notyf.Error("دخول غير مصرح به");
                    await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                    return RedirectToAction("Login", "Account");
                }
                theClass.IdSchool = school;
                _context.Add(theClass);
                await _context.SaveChangesAsync();
                _notyf.Success("تمت عملية الاضافة بنجاح");
                return RedirectToAction("ManagerMenegarClassView", "Menegar");
            }
            return View(theClass);
        }

        // GET: TheClass/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            Exception e = new Exception();
            if (id == null)
            {
                e = new Exception("التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(e, "TheClass/Edit");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                return RedirectToAction("ManagerMenegarClassView", "Menegar");
            }

            var theClass = await _context.TheClasses.FindAsync(id);
            if (theClass == null)
            {
                e = new Exception("التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(e, "TheClass/Edit");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                return RedirectToAction("ManagerMenegarClassView", "Menegar");
            }
            return View(theClass);
        }

        // POST: TheClass/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] TheClass theClass)
        {
            Exception e = new Exception();
            if (id != theClass.Id)
            {
                e = new Exception("البيانات المرسلة غير صحيحة");
                await _logger.LogAsync(e, "TheClass/Edit");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ");
                return RedirectToAction("ManagerMenegarClassView", "Menegar");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    TheClass theClass1 = await _context.TheClasses.FirstOrDefaultAsync(c => c.Id == theClass.Id);
                    if (theClass1 == null)
                    {
                        e = new Exception("تلاعب بالبيانات المرسلة للتحقق و الحفظ");
                        await _logger.LogAsync(e, "TheClass/Edit");
                        _notyf.Error(" لا يمكن التلاعب بالبيانات المرسلة للتحقق و الحفظ");
                        return View(theClass);
                    }
                    theClass1.Name = theClass.Name;
                    await _context.SaveChangesAsync();
                    _notyf.Success("تمت عملية التعديل بنجاح");
                    return RedirectToAction("ManagerMenegarClassView", "Menegar");
                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(ex, "TheClass/Edit");
                    _notyf.Error("حدث خطأ غير متوقع\nحاول مرة اخرى لاحقا");
                    return View(theClass);
                }
                
            }
            _notyf.Error("خطأ بالبيانات المدخلة");
            return View(theClass);
        }

        // GET: TheClass/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theClass = await _context.TheClasses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (theClass == null)
            {
                return NotFound();
            }

            return View(theClass);
        }

        // POST: TheClass/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var theClass = await _context.TheClasses.FindAsync(id);
            var theStudent =  _context.Students
            .Where(sc => sc.IdClass == id).ToList();
            var theTeacher =  _context.TeacherLectuerClasses
            .Where(sc => sc.IdClass == id).ToList();
            if (theClass != null)
            {
                
                foreach (var item in theTeacher)
                {
                    _context.TeacherLectuerClasses.Remove(item);
                    
                }
                _context.TheClasses.Remove(theClass);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManagerMenegarClass","Menegar");
        }

        public async Task<IActionResult> ManagerClassStudent([FromQuery]int idclass)
            {
                try{
                    var students = await _context.Students
                    .Where(ts => ts.IdClass ==idclass )
                    .Select(ts => new ClassStudentViewModel{
                        StudentName = ts.Name??"Null",
                        Email = ts.Email??"Null",
                        Phone = ts.Phone??"Null",
                        ClassName=ts.Name??"Null"
                        
                        })
                        .ToListAsync(); 
                    return View(students);
                        
                }catch(Exception e){
                    Console.WriteLine($"Error: {e.Message}");
                    return RedirectToAction(nameof(Index));
                }
            }

        [HttpGet]
        public IActionResult CreateTeacherClass(int idClass)
        {
            TheClass? nameClass = _context.TheClasses.Where(lec => lec.Id == idClass).FirstOrDefault();
            if (nameClass == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "Lectuer/CreateTeacherLectuer");
                return RedirectToAction("ManagerMenegarClassView");
            }
            ViewBag.IdClass = idClass;
            ViewBag.NameLectuer = nameClass.Name;
            List<Teacher> teacher = _context.Teachers.Where(s =>
            _context.TeacherLectuerClasses.Any(t => s.Id == t.IdTeacher && t.IdClass!=null && idClass != t.IdClass)
            && s.IdSchool == HttpContext.Session.GetInt32("School")).ToList();
            ViewData["IdTeacher"] = new SelectList(teacher, "Id", "Name");
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacherClass(int idClass,[Bind("IdTeacher,IdClass")] LectuerTeacherViewModel teacherClass)
        {
            try
            {
                if (idClass == teacherClass.IdClass)
                {
                    if (ModelState.IsValid)
                    {

                        Teacher? teacher = _context.Teachers.FirstOrDefault(t => t.Id == teacherClass.IdTeacher);
                        if (teacher != null)
                        {
                            TheClass? theClass = _context.TheClasses.FirstOrDefault(t => t.Id == teacherClass.IdClass);
                            if (theClass != null)
                            {
                                TeacherLectuerClass? TeacherInClass = _context.TeacherLectuerClasses
                                    .Where(joined =>
                                    joined.IdTeacher == teacherClass.IdTeacher
                                    && joined.IdClass == teacherClass.IdClass
                                    && joined.IdSchool == HttpContext.Session.GetInt32("School"))
                                    .FirstOrDefault();
                                if (TeacherInClass == null)
                                {
                                    TeacherLectuerClass? ontherTeacherInClass = _context.TeacherLectuerClasses
                                    .Where(joined =>
                                    joined.IdTeacher == teacherClass.IdTeacher
                                    && joined.IdClass == null
                                    && joined.IdSchool == HttpContext.Session.GetInt32("School"))
                                    .FirstOrDefault();

                                    if (ontherTeacherInClass != null)
                                    {
                                        TeacherLectuerClass? endTeacher = _context.TeacherLectuerClasses
                                            .Where(joined =>
                                            joined.IdLectuer == ontherTeacherInClass.IdLectuer
                                            && joined.IdClass == teacherClass.IdClass
                                            && joined.IdSchool == HttpContext.Session.GetInt32("School"))
                                            .FirstOrDefault();
                                        if (endTeacher == null)
                                        {
                                            ontherTeacherInClass.IdClass = teacherClass.IdClass;
                                            await _context.SaveChangesAsync();
                                            List<Student>? allStudentInClass =await _context.Students.Where(s => s.IdClass == teacherClass.IdClass).ToListAsync();
                                            foreach (Student std in allStudentInClass)
                                            {
                                                Console.WriteLine($"name student: {std.Name}");
                                                StudentLectuerTeacher studentLectuerTeacher = new StudentLectuerTeacher
                                                {
                                                    IdStudent = std.Id,
                                                    IdClass = std.IdClass,
                                                    IdSchool = std.IdSchool,
                                                    IdLectuer = ontherTeacherInClass.IdLectuer,
                                                    IdTeacher = ontherTeacherInClass.IdTeacher
                                                };
                                                _context.StudentLectuerTeachers.Add(studentLectuerTeacher);
                                            }
                                            await _context.SaveChangesAsync();
                                            _notyf.Success($"تم اضافة المعلم لتدريس الصف");
                                            return RedirectToAction("ManagerMenegarTeacherInClassView","Menegar", new { idClass = teacherClass.IdClass });
                                        }
                                        else
                                        {
                                            _notyf.Error("هناك معلم يدرس الصف بنفس المادة الخاصة بك");
                                        }

                                    }
                                    else
                                    {
                                        _notyf.Error("يجب تسجيل المادة للمعلم قبل تسجيله في الصف");
                                        return RedirectToAction("LectuerView", "Lectuer");
                                    }

                                }
                                else
                                {
                                    _notyf.Error("المعلم مسجل مسبقا لهذا الصف");
                                }

                            }
                            else
                            {
                                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                                await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "TheClass/CreateTeacherClass");
                            }
                        }
                        else
                        {
                            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                            await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "TheClass/CreateTeacherClass");
                        }
                    }
                    else
                    {
                        _notyf.Error("البيانات المدخلة خاطئة");
                    }
                }
                else
                {
                    _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                    await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "TheClass/CreateTeacherClass");
                }
                
            }
            catch (Exception ex)
            {
                _notyf.Error("حدث خطا غير متوقع\nحاول لاحقا");
                await _logger.LogAsync(ex, "TheClass/CreateTeacherClass");
            }
            return RedirectToAction(nameof(CreateTeacherClass), new { idClass = teacherClass.IdClass });
        }


        private bool TheClassExists(int id)
        {
            return _context.TheClasses.Any(e => e.Id == id);
        }
    }
}
