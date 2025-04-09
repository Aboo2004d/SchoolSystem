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
    public class StudentClassController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public StudentClassController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: StudentClass
        public async Task<IActionResult> Index()
        {
            var systemSchoolDbContext = _context.StudentClasses.Include(s => s.IdClassNavigation).Include(s => s.IdStudentNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: StudentClass/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentClass = await _context.StudentClasses
                .Include(s => s.IdClassNavigation)
                .Include(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (studentClass == null)
            {
                return NotFound();
            }

            return View(studentClass);
        }

        // GET: StudentClass/Create
        public IActionResult Create()
        {
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Name");
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
            return View();
        }

        // POST: StudentClass/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,NameStudent,IdStudent,NameClass,IdClass")] StudentClass studentClass)
        {
            if (ModelState.IsValid)
            {
                _context.Add(studentClass);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Name", studentClass.IdClass);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name", studentClass.IdStudent);
            return View(studentClass);
        }

        // GET: StudentClass/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var studentClass = await _context.StudentClasses.FindAsync(id);
            if (studentClass == null)
            {
                return NotFound();
            }
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Name", studentClass.IdClass);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name", studentClass.IdStudent);
            return View(studentClass);
        }

        // POST: StudentClass/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,NameStudent,IdStudent,NameClass,IdClass")] StudentClass studentClass)
        {
            if (id != studentClass.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(studentClass);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentClassExists(studentClass.Id))
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
            ViewData["IdClass"] = new SelectList(_context.TheClasses, "Id", "Name", studentClass.IdClass);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name", studentClass.IdStudent);
            return View(studentClass);
        }

        // GET: StudentClass/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            Console.WriteLine($"Id: "+ id);
            
            if (id == null)
            {
                return NotFound();
            }

            var studentClass = await _context.StudentClasses
                .Include(s => s.IdClassNavigation)
                .Include(s => s.IdStudentNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (studentClass == null)
            {
                return NotFound();
            }

            return View(studentClass);
        }

        // POST: StudentClass/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var studentClass = await _context.StudentClasses.FindAsync(id);
            if (studentClass == null)
            {
                Console.WriteLine("Error: studentClass is null");
                return NotFound("The record was not found.");
            }
            var idClass = studentClass.IdClass;
            Console.WriteLine($"Id: "+idClass);
            if (studentClass != null)
            {
                _context.StudentClasses.Remove(studentClass);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManagerMenegarStudentInClass","Menegar",new { idClass });
        }

        private bool StudentClassExists(int id)
        {
            return _context.StudentClasses.Any(e => e.Id == id);
        }
    }
}
