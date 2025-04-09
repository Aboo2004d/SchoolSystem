using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using NuGet.Protocol;
using SchoolSystem.Data;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    public class StudentController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public StudentController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: Student
        public async Task<IActionResult> Index()
        {
            return View(await _context.Students.ToListAsync());
        }

        // GET: Student/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // GET: Student/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Student/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Phone,Email")] Student student)
        {
            if (ModelState.IsValid)
            {
                _context.Add(student);
                await _context.SaveChangesAsync();
                return RedirectToAction("ManagerMenegarStudent","Menegar");
            }
            return View(student);
        }
        
        // GET: Student/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }
            return View(student);
        }

        // POST: Student/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Phone,Email")] Student student)
        {
            if (id != student.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(student);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!StudentExists(student.Id))
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
            return View(student);
        }

        // GET: Student/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var student = await _context.Students
                .FirstOrDefaultAsync(m => m.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            return View(student);
        }

        // POST: Student/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                var StudentInLectuer = _context.StudentLectuers
                    .Where(sl => sl.IdStudent == id);
                    Console.WriteLine($"Student Lectuer: {StudentInLectuer}");
                var StudentInClass = _context.StudentClasses
                    .Where(sl => sl.IdStudent == id);
                    Console.WriteLine($"Student Class: {StudentInClass}");
                var StudentInTeacher = _context.StudentTeachers
                    .Where(sl => sl.IdStudent == id);
                Console.WriteLine($"Student Teacher: {StudentInTeacher}");
                foreach (var item in StudentInLectuer)
                {
                    _context.StudentLectuers.Remove(item);
                }
                foreach (var item in StudentInClass)
                {
                    _context.StudentClasses.Remove(item);
                }
                foreach (var item in StudentInTeacher)
                {
                    _context.StudentTeachers.Remove(item);
                }
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction("ManagerMenegarStudent","Menegar");
        }
        public async Task<IActionResult> Delete1(int? id)
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
        [HttpPost, ActionName("Delete1")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete1Confirmed(int id)
        {
            var student = await _context.Students.FindAsync(id);
            if (student != null)
            {
                
                _context.Students.Remove(student);
            }

            await _context.SaveChangesAsync();
            
            return RedirectToAction("ManagerStudentLectuer", student);
        }
        public async Task<IActionResult> ManagerStudentLectuer([FromQuery]int idstudent)
        {
            try{
                Console.WriteLine($"idStudent: {idstudent}");
                var students = await _context.StudentLectuers
                .Where(ts => ts.IdStudent ==idstudent )
                .Include(ts => ts.IdStudentNavigation)
                .Include(sl => sl.IdLectuerNavigation)
                .ThenInclude(lt => lt.TeacherLectuers)
                .Select(ts => new StudentLectuerViewModel{
                    Id= ts.Id,
                    TeacherName = ts.IdLectuerNavigation.TeacherLectuers.Select(st => st.IdTeacherNavigation.Name)
                    .FirstOrDefault(),
                    StudentName = ts.IdStudentNavigation.Name,
                    IdStudent = ts.IdStudentNavigation.Id,
                    ClassroomName = ts.IdStudentNavigation.StudentClasses.Select(sc => sc.IdClassNavigation.Name)
                    .FirstOrDefault(),
                    LectureName = ts.IdLectuerNavigation.Name
                    })
                    .ToListAsync(); 
                return View(students);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> StudentLectuerReturn([FromQuery]int idstudent)
        {
            try{
                Console.WriteLine($"idStudent: {idstudent}");
                var students = await _context.StudentLectuers
                .Where(ts => ts.IdStudent ==idstudent )
                .Include(ts => ts.IdStudentNavigation)
                .Include(sl => sl.IdLectuerNavigation)
                .Include(lt => lt.IdLectuerNavigation.TeacherLectuers)
                .Select(ts => new StudentLectuerReturnViewModel{
                    Id= ts.Id,
                    TeacherName = ts.IdLectuerNavigation.TeacherLectuers.Select(st => st.IdTeacherNavigation.Name)
                    .FirstOrDefault(),
                    StudentName = ts.IdStudentNavigation.Name,
                    IdStudent = ts.IdStudentNavigation.Id,
                    ClassroomName = ts.IdStudentNavigation.StudentClasses.Select(sc => sc.IdClassNavigation.Name)
                    .FirstOrDefault(),
                    LectureName = ts.IdLectuerNavigation.Name
                    })
                    .ToListAsync(); 
                return View(students);
                    
            }catch(Exception e){
                Console.WriteLine($"Error: {e.Message}");
                return RedirectToAction(nameof(Index));
            }
        }
        
        private bool StudentExists(int id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}
