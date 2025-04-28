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
    public class MenegarController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public MenegarController(SystemSchoolDbContext context)
        {
            _context = context;
        }
        [AuthorizeRoles("admin")]
        // GET: Menegar
        public async Task<IActionResult> Index()
        {
            return View(await _context.Menegars.ToListAsync());
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
        public async Task<IActionResult> Create([Bind("Id,Name,Phone,Email")] Menegar menegar)
        {
            if (ModelState.IsValid)
            {
                _context.Add(menegar);
                await _context.SaveChangesAsync();
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
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Phone,Email")] Menegar menegar)
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
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerMenegarStudent([FromQuery]int id)
        {
            try{
                var allstudents = await _context.Students
                    .GroupJoin(
                        _context.Grades.Where(g => g.Total != null),
                        student => student.Id,
                        grade => grade.IdStudent,
                        (student, grades) => new { student, grades })
                        .Select(x => new MenegarStudentViewModel
                        {
                            IdStudent = x.student.Id,
                            StudentName = x.student.Name,
                            ClassroomName = x.student.StudentClasses.Any()
                                ? x.student.StudentClasses.First().IdClassNavigation.Name
                                : null,
                            Average = x.grades.Any() ? x.grades.Average(g => g.Total.Value) : 0,
                            Day = _context.Attendances
                                .Where(att => att.IdStudent == x.student.Id && att.AttendanceStatus == "1").Count(),
                            TotalDay = _context.Attendances
                                .Where(att => att.IdStudent == x.student.Id).Count()
                            
                        })
                    .ToListAsync();

                    if (!allstudents.Any()){
                        return RedirectToAction(nameof(Index));
                    }
                    foreach (var student in allstudents)
                    {
                        Console.WriteLine($"Student: {student.StudentName}, Average: {student.Average}, Day: {student.Day}, TotalDay: {student.TotalDay}");
                    }
                    
                return View(allstudents);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerMenegarTeacher([FromQuery]int id)
        {
            try{
                var teachers = await _context.Teachers
                .Select(ts => new MenegarTeacherViewModel{
                    TeacherName = ts.Name,
                    EmailAddress = ts.Email,
                    Phone = ts.Phone
                    })
                    .ToListAsync();
                    if (!teachers.Any()){
                        return RedirectToAction(nameof(Index));
                    }
                return View(teachers);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerMenegarClass([FromQuery] int id) 
        {
            try{
                var classes = await _context.TheClasses
                .Select(ts => new MenegarClassViewModel{
                    id = ts.Id,
                    ClassroomName = ts.Name,
                    NumberOfStudents = ts.StudentClasses.Select(sc => sc.IdStudent).Distinct().Count()
                    })
                    .ToListAsync();
                    if (!classes.Any()){
                        return RedirectToAction(nameof(Index));
                    }
                return View(classes);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
        [AuthorizeRoles("admin")]
        public async Task<IActionResult> ManagerMenegarStudentInClass([FromQuery]int idClass)
        {
            try{
                var students = await _context.StudentClasses
                .Where(sc => sc.IdClass == idClass)
                .Include(sc => sc.IdClassNavigation)
                .Include(sc => sc.IdStudentNavigation)
                .Select(ts => new ManagerMenegarStudentInClassViewModel{
                    Id= ts.Id,
                    IdStudent = ts.IdStudentNavigation.Id,
                    IdClass = ts.IdClassNavigation.Id,
                    StudentName = ts.IdStudentNavigation.Name,
                    ClassroomName = ts.IdClassNavigation.Name
                    })
                    .ToListAsync(); 
                if (!students.Any())
                {
                    students.Add(new ManagerMenegarStudentInClassViewModel
                    {
                        IdClass = idClass
                    });
                }
                    
                return View(students);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
        [AuthorizeRoles("admin")]
        public IActionResult CreateStudentWithClass(int idClass)
        {
            Console.WriteLine($"idClass: {@idClass}");
            var @class = _context.TheClasses.FirstOrDefault(s => s.Id == idClass);
            var idStudent = _context.Students.FirstOrDefault();
            var StudentClass = _context.StudentClasses.FirstOrDefault();
            Console.WriteLine($"Class: {@class.Name}");
            if (@class == null)
            {
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Cls = new SelectList(_context.TheClasses, "Id", "Name", idClass);
            ViewBag.IdClass = idClass;
            ViewBag.IdStudent = new SelectList(_context.Students, "Id", "Name");
            var studentClass = new StudentClass
                {
                    IdClass = idClass
                };

                return View(studentClass);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateStudentForClass([Bind("IdStudent,IdClass")] StudentClass studentClass)
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
                return RedirectToAction(nameof(CreateStudentWithClass), new { idClass = studentClass.IdClass });
            }

            try
            {
                
                // إضافة البيانات إلى قاعدة البيانات
                _context.Add(studentClass);
                await _context.SaveChangesAsync();
                // استعلام الطلاب في الصف
                var students = await _context.StudentClasses
                    .Where(ts => ts.IdClass == studentClass.IdClass)
                    .Include(ts => ts.IdStudentNavigation)
                    .Include(ts => ts.IdClassNavigation)
                    .Select(ts => new StudentClassViewModels
                    {
                        Id = ts.Id,
                        StudentName = ts.IdStudentNavigation.Name,
                        ClassroomName = ts.IdClassNavigation.Name
                    })
                    .ToListAsync();

                // الحصول على الرابط السابق من الهيدر
                return RedirectToAction("ManagerMenegarStudentInClass", "Menegar", new { idClass = studentClass.IdClass });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                ModelState.AddModelError("", "حدث خطأ أثناء حفظ البيانات. الرجاء المحاولة مرة أخرى.");

                // إعادة المستخدم إلى نفس الصفحة مع البيانات الحالية
                return RedirectToAction(nameof(CreateStudentWithClass), new { idClass = studentClass.IdClass });
            }
        }
        [AuthorizeRoles("admin")]
        public IActionResult AddStudentWithClass(int idstudent)
        {
            var @class = _context.TheClasses.Any();
            if (@class == null)
            {
                return RedirectToAction(nameof(Index));
            }
            ViewBag.IdClass = new SelectList(_context.TheClasses, "Id", "Name");
            ViewBag.IdStudent = new SelectList(_context.Students, "Id", "Name",idstudent);
            ViewBag.IdStu = idstudent;


                return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddStudentWithClass([Bind("IdStudent,IdClass")] StudentClass studentClass)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine($"Validation Error: {error}");
                }

                // عند وجود خطأ، يتم إعادة التوجيه إلى نفس الصفحة مع تمرير البيانات
                return RedirectToAction(nameof(AddStudentWithClass));
            }else{
                    _context.StudentClasses.Add(studentClass);
                    await _context.SaveChangesAsync();
                    return RedirectToAction("ManagerMenegarStudent","Menegar");
                }
        }


        [AuthorizeRoles("admin")]
        private bool MenegarExists(int id)
        {
            return _context.Menegars.Any(e => e.Id == id);
        }
    }
}
