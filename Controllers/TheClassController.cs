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
    public class TheClassController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public TheClassController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: TheClass
        public async Task<IActionResult> Index()
        {
            return View(await _context.TheClasses.ToListAsync());
        }

        // GET: TheClass/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theClass = await _context.TheClasses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (theClass == null)
            {
                return NotFound();
            }

            return View(theClass);
        }

        // GET: TheClass/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: TheClass/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name")] TheClass theClass)
        {
            if (ModelState.IsValid)
            {
                _context.Add(theClass);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManagerMenegarClass","Menegar");
            }
            return View(theClass);
        }

        // GET: TheClass/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theClass = await _context.TheClasses.FindAsync(id);
            if (theClass == null)
            {
                return NotFound();
            }
            return View(theClass);
        }

        // POST: TheClass/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name")] TheClass theClass)
        {
            if (id != theClass.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(theClass);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TheClassExists(theClass.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("ManagerMenegarClass","Menegar");
            }
            return View(theClass);
        }

        // GET: TheClass/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var theClass = await _context.TheClasses
                .FirstOrDefaultAsync(m => m.Id == id);
            if (theClass == null)
            {
                return NotFound();
            }

            return View(theClass);
        }

        // POST: TheClass/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var theClass = await _context.TheClasses.FindAsync(id);
            var theStudent =  _context.StudentClasses
            .Where(sc => sc.IdClass == id).ToList();
            var theTeacher =  _context.TeacherClasses
            .Where(sc => sc.IdClass == id).ToList();
            var theLectuer =  _context.ClassLectuers
            .Where(sc => sc.IdClass == id).ToList();
            if (theClass != null)
            {
                foreach (var item in theLectuer)
                {
                    _context.ClassLectuers.Remove(item);
                    
                }
                foreach (var item in theTeacher)
                {
                    _context.TeacherClasses.Remove(item);
                    
                }
                foreach (var item in theStudent)
                {
                    _context.StudentClasses.Remove(item);
                    
                }
                _context.TheClasses.Remove(theClass);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManagerMenegarClass","Menegar");
        }

        public async Task<IActionResult> ManagerClassStudent([FromQuery]int idclass)
            {
                try{
                    var students = await _context.StudentClasses
                    .Where(ts => ts.IdClass ==idclass )
                    .Include(ts => ts.IdStudentNavigation)
                    .Select(ts => new ClassStudentViewModel{
                        StudentName = ts.IdStudentNavigation.Name,
                        Email = ts.IdStudentNavigation.Email,
                        Phone = ts.IdStudentNavigation.Phone,
                        ClassName=ts.IdClassNavigation.Name
                        
                        })
                        .ToListAsync(); 
                    return View(students);
                        
                }catch(Exception e){
                    Console.WriteLine($"Error: {e.Message}");
                    return RedirectToAction(nameof(Index));
                }
            }

        private bool TheClassExists(int id)
        {
            return _context.TheClasses.Any(e => e.Id == id);
        }
    }
}
