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
    public class TeacherClassController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public TeacherClassController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: TeacherClass
        public async Task<IActionResult> Index()
        {
            var systemSchoolDbContext = _context.TeacherClasses.Include(t => t.IdClassNavigation).Include(t => t.IdTeacherNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: TeacherClass/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherClass = await _context.TeacherClasses
                .Include(t => t.IdClassNavigation)
                .Include(t => t.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacherClass == null)
            {
                return NotFound();
            }

            return View(teacherClass);
        }

        // GET: TeacherClass/Create
        public IActionResult Create()
        {
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id");
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id");
            return View();
        }

        // POST: TeacherClass/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NameTeacher,IdTeacher,NameClass,IdClass")] TeacherClass teacherClass)
        {
            if (ModelState.IsValid)
            {
                _context.Add(teacherClass);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", teacherClass.IdClass);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", teacherClass.IdTeacher);
            return View(teacherClass);
        }

        public IActionResult CreateTeacherClass(int idTeacher, int idLectuer)
        {
            Console.WriteLine($"Lectuer: {idLectuer}");
            Console.WriteLine($"Teacher: {idTeacher}");
            ViewBag.IdTeach = new SelectList(_context.Teachers, "Id", "Name",idTeacher);
            ViewBag.IdTeacher = idTeacher;
            ViewBag.IdClass = new SelectList(_context.TheClasses, "Id", "Name");
            ViewBag.IdLectuer = idLectuer;
            return View();
        }

        // POST: TeacherLectuer/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacherToClass([Bind("IdTeacher,IdLectuer,IdClass")] LectuerTeacherViewModel teacherLectuer)
        {
            
            if (ModelState.IsValid)
            {
                var teacherclass = new TeacherClass{
                    IdClass = teacherLectuer.IdClass,
                    IdTeacher = teacherLectuer.IdTeacher
                };
                _context.TeacherClasses.Add(teacherclass);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManagerTeacherLectuer","Lectuer", new { idLectuer = teacherLectuer.IdLectuer });
            }
            ViewBag.IdTeach = new SelectList(_context.Teachers, "Id", "Name",teacherLectuer.IdTeacher);
            ViewBag.IdTeacher = teacherLectuer.IdTeacher;
            ViewBag.IdClass = new SelectList(_context.TheClasses, "Id", "Name");
            ViewBag.IdLectuer = teacherLectuer.IdLectuer;
            return View(teacherLectuer);
        }

        // GET: TeacherClass/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherClass = await _context.TeacherClasses.FindAsync(id);
            if (teacherClass == null)
            {
                return NotFound();
            }
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", teacherClass.IdClass);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", teacherClass.IdTeacher);
            return View(teacherClass);
        }

        // POST: TeacherClass/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NameTeacher,IdTeacher,NameClass,IdClass")] TeacherClass teacherClass)
        {
            if (id != teacherClass.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(teacherClass);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TeacherClassExists(teacherClass.Id))
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
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Id", teacherClass.IdClass);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", teacherClass.IdTeacher);
            return View(teacherClass);
        }

        // GET: TeacherClass/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var teacherClass = await _context.TeacherClasses
                .Include(t => t.IdClassNavigation)
                .Include(t => t.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (teacherClass == null)
            {
                return NotFound();
            }

            return View(teacherClass);
        }

        // POST: TeacherClass/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var teacherClass = await _context.TeacherClasses.FindAsync(id);
            if (teacherClass != null)
            {
                _context.TeacherClasses.Remove(teacherClass);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool TeacherClassExists(int id)
        {
            return _context.TeacherClasses.Any(e => e.Id == id);
        }
    }
}
