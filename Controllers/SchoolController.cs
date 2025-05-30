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
    public class SchoolController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public SchoolController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: School
        public async Task<IActionResult> Index()
        {
            return View(await _context.StatusSchools.ToListAsync());
        }

        // GET: School/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var statusSchool = await _context.StatusSchools
                .FirstOrDefaultAsync(m => m.Id == id);
            if (statusSchool == null)
            {
                return NotFound();
            }

            return View(statusSchool);
        }

        // GET: School/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: School/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Condition,TheType")] StatusSchool statusSchool)
        {
            if (ModelState.IsValid)
            {
                _context.Add(statusSchool);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(statusSchool);
        }

        // GET: School/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var statusSchool = await _context.StatusSchools.FindAsync(id);
            if (statusSchool == null)
            {
                return NotFound();
            }
            return View(statusSchool);
        }

        // POST: School/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Condition,TheType")] StatusSchool statusSchool)
        {
            if (id != statusSchool.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(statusSchool);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StatusSchoolExists(statusSchool.Id))
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
            return View(statusSchool);
        }

        // GET: School/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var statusSchool = await _context.StatusSchools
                .FirstOrDefaultAsync(m => m.Id == id);
            if (statusSchool == null)
            {
                return NotFound();
            }

            return View(statusSchool);
        }

        // POST: School/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var statusSchool = await _context.StatusSchools.FindAsync(id);
            if (statusSchool != null)
            {
                _context.StatusSchools.Remove(statusSchool);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool StatusSchoolExists(int id)
        {
            return _context.StatusSchools.Any(e => e.Id == id);
        }
    }
}
