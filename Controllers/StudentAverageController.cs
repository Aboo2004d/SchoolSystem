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
    public class StudentAverageController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public StudentAverageController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: StudentAverage
        public async Task<IActionResult> Index()
        {
            var systemSchoolDbContext = _context.StudentAverages.Include(s => s.IdClassNavigation).Include(s => s.IdStudentNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: StudentAverage/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentAverage = await _context.StudentAverages
                .Include(s => s.IdClassNavigation)
                .Include(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(m => m.IdStudentAvg == id);
            if (studentAverage == null)
            {
                return NotFound();
            }

            return View(studentAverage);
        }

        // GET: StudentAverage/Create
        public IActionResult Create()
        {
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id");
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id");
            return View();
        }

        // POST: StudentAverage/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("AverageGrade,IdStudentAvg,IdStudent,IdClass")] StudentAverage studentAverage)
        {
            if (ModelState.IsValid)
            {
                _context.Add(studentAverage);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", studentAverage.IdClass);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", studentAverage.IdStudent);
            return View(studentAverage);
        }

        // GET: StudentAverage/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentAverage = await _context.StudentAverages.FindAsync(id);
            if (studentAverage == null)
            {
                return NotFound();
            }
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", studentAverage.IdClass);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", studentAverage.IdStudent);
            return View(studentAverage);
        }

        // POST: StudentAverage/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AverageGrade,IdStudentAvg,IdStudent,IdClass")] StudentAverage studentAverage)
        {
            if (id != studentAverage.IdStudentAvg)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studentAverage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentAverageExists(studentAverage.IdStudentAvg))
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
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", studentAverage.IdClass);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", studentAverage.IdStudent);
            return View(studentAverage);
        }

        // GET: StudentAverage/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentAverage = await _context.StudentAverages
                .Include(s => s.IdClassNavigation)
                .Include(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(m => m.IdStudentAvg == id);
            if (studentAverage == null)
            {
                return NotFound();
            }

            return View(studentAverage);
        }

        // POST: StudentAverage/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var studentAverage = await _context.StudentAverages.FindAsync(id);
            if (studentAverage != null)
            {
                _context.StudentAverages.Remove(studentAverage);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentAverageExists(int id)
        {
            return _context.StudentAverages.Any(e => e.IdStudentAvg == id);
        }
    }
}
