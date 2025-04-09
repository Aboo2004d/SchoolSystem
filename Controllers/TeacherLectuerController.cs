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
    public class TeacherLectuerController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public TeacherLectuerController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: TeacherLectuer
        public async Task<IActionResult> Index()
        {
            var systemSchoolDbContext = _context.TeacherLectuers.Include(t => t.IdLectuerNavigation).Include(t => t.IdTeacherNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: TeacherLectuer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherLectuer = await _context.TeacherLectuers
                .Include(t => t.IdLectuerNavigation)
                .Include(t => t.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacherLectuer == null)
            {
                return NotFound();
            }

            return View(teacherLectuer);
        }

        // GET: TeacherLectuer/Create
        public IActionResult Create(int idLectuer)
        {
            var lectuers = _context.Lectuers.FirstOrDefault(s => s.Id == idLectuer);
            ViewBag.Lec = new SelectList(_context.Lectuers, "Id", "Name",idLectuer);
            ViewBag.IdLectuer = idLectuer;
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name");
            return View();
        }

        // POST: TeacherLectuer/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NameTeacher,IdTeacher,NameLectuer,IdLectuer")] TeacherLectuer teacherLectuer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(teacherLectuer);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManagerLectuer","Lectuer", new { idLectuer = teacherLectuer.IdLectuer });
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name", teacherLectuer.IdLectuer);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name", teacherLectuer.IdTeacher);
            return View(teacherLectuer);
        }

        // GET: TeacherLectuer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherLectuer = await _context.TeacherLectuers.FindAsync(id);
            if (teacherLectuer == null)
            {
                return NotFound();
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name", teacherLectuer.IdLectuer);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name", teacherLectuer.IdTeacher);
            return View(teacherLectuer);
        }

        // POST: TeacherLectuer/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NameTeacher,IdTeacher,NameLectuer,IdLectuer")] TeacherLectuer teacherLectuer)
        {
            if (id != teacherLectuer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teacherLectuer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherLectuerExists(teacherLectuer.Id))
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
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name", teacherLectuer.IdLectuer);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name", teacherLectuer.IdTeacher);
            return View(teacherLectuer);
        }

        // GET: TeacherLectuer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherLectuer = await _context.TeacherLectuers
                .Include(t => t.IdLectuerNavigation)
                .Include(t => t.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacherLectuer == null)
            {
                return NotFound();
            }

            return View(teacherLectuer);
        }

        // POST: TeacherLectuer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacherLectuer = await _context.TeacherLectuers.FindAsync(id);
            if (teacherLectuer != null)
            {
                _context.TeacherLectuers.Remove(teacherLectuer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManagerTeacherLectuer", new { idLectuer = teacherLectuer.IdLectuer });
        }

        private bool TeacherLectuerExists(int id)
        {
            return _context.TeacherLectuers.Any(e => e.Id == id);
        }
    }
}
