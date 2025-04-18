using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;

namespace SchoolSystem.Controllers
{
    public class GradeController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public GradeController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: Grade
        public async Task<IActionResult> Index()
        {
            var systemSchoolDbContext = _context.Grades.Include(g => g.IdLectuerNavigation).Include(g => g.IdStudentNavigation).Include(g => g.IdTeacherNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: Grade/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.IdLectuerNavigation)
                .Include(g => g.IdStudentNavigation)
                .Include(g => g.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.GradesId == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // GET: Grade/Create
        public IActionResult Create()
        {
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id");
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id");
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id");
            return View();
        }

        // POST: Grade/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("GradesId,FirstMonth,Mid,SecondMonth,Activity,Final,Total,IdStudent,IdTeacher,IdLectuer")] Grade grade)
        {
            if (ModelState.IsValid)
            {
                _context.Add(grade);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", grade.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", grade.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", grade.IdTeacher);
            return View(grade);
        }

        // GET: Grade/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
            {
                return NotFound();
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", grade.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", grade.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", grade.IdTeacher);
            return View(grade);
        }

        // POST: Grade/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("GradesId,FirstMonth,Mid,SecondMonth,Activity,Final,Total,IdStudent,IdTeacher,IdLectuer")] Grade grade)
        {
            if (id != grade.GradesId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(grade);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GradeExists(grade.GradesId))
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
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", grade.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", grade.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", grade.IdTeacher);
            return View(grade);
        }

        // GET: Grade/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.IdLectuerNavigation)
                .Include(g => g.IdStudentNavigation)
                .Include(g => g.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.GradesId == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // POST: Grade/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool GradeExists(int id)
        {
            return _context.Grades.Any(e => e.GradesId == id);
        }
    }
}
