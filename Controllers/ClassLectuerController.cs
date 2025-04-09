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
    public class ClassLectuerController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public ClassLectuerController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: ClassLectuer
        public async Task<IActionResult> Index()
        {
            var systemSchoolDbContext = _context.ClassLectuers.Include(c => c.IdClassNavigation).Include(c => c.IdLectuerNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: ClassLectuer/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classLectuer = await _context.ClassLectuers
                .Include(c => c.IdClassNavigation)
                .Include(c => c.IdLectuerNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classLectuer == null)
            {
                return NotFound();
            }

            return View(classLectuer);
        }

        // GET: ClassLectuer/Create
        public IActionResult Create()
        {
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id");
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id");
            return View();
        }

        // POST: ClassLectuer/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NameClass,IdClass,NameLectuer,IdLectuer")] ClassLectuer classLectuer)
        {
            if (ModelState.IsValid)
            {
                _context.Add(classLectuer);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", classLectuer.IdClass);
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", classLectuer.IdLectuer);
            return View(classLectuer);
        }

        // GET: ClassLectuer/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classLectuer = await _context.ClassLectuers.FindAsync(id);
            if (classLectuer == null)
            {
                return NotFound();
            }
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", classLectuer.IdClass);
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", classLectuer.IdLectuer);
            return View(classLectuer);
        }

        // POST: ClassLectuer/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NameClass,IdClass,NameLectuer,IdLectuer")] ClassLectuer classLectuer)
        {
            if (id != classLectuer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(classLectuer);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ClassLectuerExists(classLectuer.Id))
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
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", classLectuer.IdClass);
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", classLectuer.IdLectuer);
            return View(classLectuer);
        }

        // GET: ClassLectuer/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var classLectuer = await _context.ClassLectuers
                .Include(c => c.IdClassNavigation)
                .Include(c => c.IdLectuerNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (classLectuer == null)
            {
                return NotFound();
            }

            return View(classLectuer);
        }

        // POST: ClassLectuer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var classLectuer = await _context.ClassLectuers.FindAsync(id);
            if (classLectuer != null)
            {
                _context.ClassLectuers.Remove(classLectuer);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ClassLectuerExists(int id)
        {
            return _context.ClassLectuers.Any(e => e.Id == id);
        }
    }
}
