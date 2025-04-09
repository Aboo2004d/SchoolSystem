using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    public class StudentLectuerController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public StudentLectuerController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: StudentLectuer
        public async Task<IActionResult> Index()
        {
            var systemSchoolDbContext = _context.StudentLectuers.Include(s => s.IdLectuerNavigation).Include(s => s.IdStudentNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: StudentLectuer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentLectuer = await _context.StudentLectuers
                .Include(s => s.IdLectuerNavigation)
                .Include(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (studentLectuer == null)
            {
                return NotFound();
            }

            return View(studentLectuer);
        }

        // GET: StudentLectuer/Create
        public IActionResult Create()
        {
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name");
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
            return View();
        }

        // POST: StudentLectuer/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NameStudent,IdStudent,NameLectuer,IdLectuer")] StudentLectuer studentLectuer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(studentLectuer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", studentLectuer.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", studentLectuer.IdStudent);
            return View(studentLectuer);
        }

        public IActionResult CreateLectuerStudentWithStudentId(int idStudent)
        {
            var student = _context.Students.FirstOrDefault(s => s.Id == idStudent);
            
            if (student == null)
            {
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Stu = new SelectList(_context.Students, "Id", "Name",idStudent);
            ViewBag.IdStudent = idStudent;
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name");

            return View();
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLectuerStudentForNewStudent([Bind("IdStudent,IdLectuer")] StudentLectuer studentLectuer)
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
                return RedirectToAction(nameof(CreateLectuerStudentWithStudentId), new { idStudent = studentLectuer.IdStudent });
            }

            try
            {
                // إضافة البيانات إلى قاعدة البيانات
                _context.Add(studentLectuer);
                await _context.SaveChangesAsync();
                var students = await _context.StudentLectuers
                .Where(ts => ts.IdStudent ==studentLectuer.IdStudent )
                .Include(ts => ts.IdStudentNavigation)
                .Include(sl => sl.IdLectuerNavigation)
                .Include(lt => lt.IdLectuerNavigation.TeacherLectuers)
                .Select(ts => new StudentLectuerViewModel{
                    Id=ts.Id,
                    TeacherName = ts.IdLectuerNavigation.TeacherLectuers.Select(st => st.IdTeacherNavigation.Name)
                    .FirstOrDefault(),
                    StudentName = ts.IdStudentNavigation.Name,
                    IdStudent = ts.IdStudentNavigation.Id,
                    ClassroomName = ts.IdStudentNavigation.StudentClasses.Select(sc => sc.IdClassNavigation.Name)
                    .FirstOrDefault(),
                    LectureName = ts.IdLectuerNavigation.Name
                    })
                    .ToListAsync(); 

                // الحصول على الرابط السابق من الهيدر
                return RedirectToAction("ManagerStudentLectuer", "Student", new { idStudent = studentLectuer.IdStudent });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Database Error: {ex.Message}");
                ModelState.AddModelError("", "حدث خطأ أثناء حفظ البيانات. الرجاء المحاولة مرة أخرى.");

                // إعادة المستخدم إلى نفس الصفحة مع البيانات الحالية
                return RedirectToAction(nameof(CreateLectuerStudentWithStudentId), new { idStudent = studentLectuer.IdStudent });
            }
        }

        



        // GET: StudentLectuer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentLectuer = await _context.StudentLectuers.FindAsync(id);
            if (studentLectuer == null)
            {
                return NotFound();
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", studentLectuer.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", studentLectuer.IdStudent);
            return View(studentLectuer);
        }

        // POST: StudentLectuer/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NameStudent,IdStudent,NameLectuer,IdLectuer")] StudentLectuer studentLectuer)
        {
            if (id != studentLectuer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studentLectuer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentLectuerExists(studentLectuer.Id))
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
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", studentLectuer.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", studentLectuer.IdStudent);
            return View(studentLectuer);
        }

        // GET: StudentLectuer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentLectuer = await _context.StudentLectuers
                .Include(s => s.IdLectuerNavigation)
                .Include(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (studentLectuer == null)
            {
                return NotFound();
            }

            return View(studentLectuer);
        }

        // POST: StudentLectuer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            Console.WriteLine($"Id: {id}");
            var studentLectuer = await _context.StudentLectuers.FindAsync(id);
            var idLec = studentLectuer.IdLectuer;
            if (studentLectuer != null)
            {
                Console.WriteLine($"Id Lectuer: {idLec}");
                _context.StudentLectuers.Remove(studentLectuer);
            }
            await _context.SaveChangesAsync();
            return RedirectToAction("ManagerLectuer","Lectuer", new{idLectuer = idLec});
        }

        

        private bool StudentLectuerExists(int id)
        {
            return _context.StudentLectuers.Any(e => e.Id == id);
        }
    }
}
