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
    public class StudentTeacherController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public StudentTeacherController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: StudentTeacher
        public async Task<IActionResult> Index()
        {
            var systemSchoolDbContext = _context.StudentTeachers.Include(s => s.IdStudentNavigation).Include(s => s.IdTeacherNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: StudentTeacher/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentTeacher = await _context.StudentTeachers
                .Include(s => s.IdStudentNavigation)
                .Include(s => s.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (studentTeacher == null)
            {
                return NotFound();
            }

            return View(studentTeacher);
        }

        // GET: StudentTeacher/Create
        public IActionResult Create()
        {
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name");
            return View();
        }

        // POST: StudentTeacher/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NameStudent,IdStudent,NameTeacher,IdTeacher")] StudentTeacher studentTeacher)
        {
            if (ModelState.IsValid)
            {
                _context.Add(studentTeacher);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name", studentTeacher.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name", studentTeacher.IdTeacher);
            return View(studentTeacher);
        }

        // GET: StudentTeacher/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentTeacher = await _context.StudentTeachers.FindAsync(id);
            if (studentTeacher == null)
            {
                return NotFound();
            }
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", studentTeacher.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", studentTeacher.IdTeacher);
            return View(studentTeacher);
        }

        // POST: StudentTeacher/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NameStudent,IdStudent,NameTeacher,IdTeacher")] StudentTeacher studentTeacher)
        {
            if (id != studentTeacher.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studentTeacher);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentTeacherExists(studentTeacher.Id))
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
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", studentTeacher.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", studentTeacher.IdTeacher);
            return View(studentTeacher);
        }

        // GET: StudentTeacher/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentTeacher = await _context.StudentTeachers
                .Include(s => s.IdStudentNavigation)
                .Include(s => s.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (studentTeacher == null)
            {
                return NotFound();
            }

            return View(studentTeacher);
        }

        // POST: StudentTeacher/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var studentTeacher = await _context.StudentTeachers.FindAsync(id);
            if (studentTeacher != null)
            {
                _context.StudentTeachers.Remove(studentTeacher);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StudentTeacherExists(int id)
        {
            return _context.StudentTeachers.Any(e => e.Id == id);
        }
    }
}
