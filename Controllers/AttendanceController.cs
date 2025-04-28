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
    public class AttendanceController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public AttendanceController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: Attendance
        public async Task<IActionResult> Index(int idTeacher)
        {
            var teacher = await _context.Teachers.FindAsync(idTeacher);
            var systemSchoolDbContext =await _context.Attendances./*Where(a=> a.IdTeacher == idTeacher ).*/Include(a => a.IdLectuerNavigation).Include(a => a.IdStudentNavigation).Include(a => a.IdTeacherNavigation).Include(a => a.IdClassNavigation)
            .ToListAsync();
            ViewData["Teacher"] = teacher.Name;
            ViewData["idTeacher"] = teacher.Id;
            return View(systemSchoolDbContext);
        }

        // GET: Attendance/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.IdLectuerNavigation)
                .Include(a => a.IdStudentNavigation)
                .Include(a => a.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // GET: Attendance/Create
        [HttpGet]
        public async Task<IActionResult> Create(int idLectuer, int idTeacher, int idClass)
        {
            Console.WriteLine($"Lectuer: {idLectuer}");
            Console.WriteLine($"Teacher: {idTeacher}");
            Console.WriteLine($"Class: {idClass}");
            var lectuer = _context.TeacherLectuers
                .Where(tl => tl.IdTeacher == idTeacher && tl.IdLectuer == idLectuer)
                .FirstOrDefault();
            if (lectuer != null )
            {
                var teacher = _context.TeacherClasses
                    .Where(tc => tc.IdTeacher == idTeacher && tc.IdClass == idClass)
                    .FirstOrDefault();
                if (teacher != null){
                    ViewData["DateAndTime"] = DateOnly.FromDateTime(DateTime.Now);
                    ViewData["Status"] = new SelectList(new List<SelectListItem> {
                        new SelectListItem { Text = "Present", Value = "1" },
                        new SelectListItem { Text = "Absent", Value = "0" },
                        new SelectListItem { Text = "Excused", Value = "m" }
                    }, "Value", "Text");
                    ViewData["IdLectuer"] = idLectuer;
                    ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
                    ViewData["IdTeacher"] = idTeacher;
                    ViewData["IdClass"] = idClass;
                    var student = await _context.StudentTeachers
                        .Where(t => t.IdTeacher == lectuer.IdTeacher && t.IdTeacher == teacher.IdTeacher)
                        .Include(st => st.IdStudentNavigation) 
                        .ToListAsync();
                    foreach(var i in student){
                        Console.WriteLine($"Student: {i.IdStudentNavigation.Name}");
                    }
                    if (student == null)
                    {
                        return NotFound("Student not found.");
                    }
                    ViewData["Students"] = student;
                    
                    return View();

                }else{
                    return NotFound("Teacher not found.");
                }
            }else return NotFound("Lectuer not found.");
            
        }

        // POST: Attendance/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        
        public async Task<IActionResult> Create(List<Attendance> Attendances)
        {
            Console.WriteLine(1);
            Console.WriteLine($"Ids Teacher: {Attendances[0].IdTeacher}");
            try{
                

                foreach(var item in ModelState.Values)
                {
                    foreach(var error in item.Errors)
                    {
                        Console.WriteLine("Error1: "+error.ErrorMessage);
                    }
                }
                Console.WriteLine(2);
                if (ModelState.IsValid){
                    Console.WriteLine(3);
                    Console.WriteLine($"Attendance: {Attendances.Count}");
                    foreach(var i in Attendances){
                        Console.WriteLine(4);
                        Console.WriteLine($"Student: {i.IdStudent}");
                        Console.WriteLine($"Status: {i.AttendanceStatus}");
                        Console.WriteLine($"Date: {i.DateAndTime}");
                        Console.WriteLine($"IdTeacher: {i.IdTeacher}");
                        Console.WriteLine($"IdLectuer: {i.IdLectuer}");
                        Console.WriteLine($"IdClass: {i.IdClass}");
                        Console.WriteLine($"Excuse: {i.Excuse}");
                        await _context.AddRangeAsync(i);
                    }
                    await _context.SaveChangesAsync();
                    return RedirectToAction("Index", new { idTeacher = Attendances[0].IdTeacher });
                }
                ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name");
                ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
                ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name");
                ViewData["DateAndTime"] = DateOnly.FromDateTime(DateTime.Now);
                ViewData["Status"] = new SelectList(new List<SelectListItem> {  
                        new SelectListItem { Text = "Present", Value = "1" },
                        new SelectListItem { Text = "Absent", Value = "0" },
                        new SelectListItem { Text = "Excused", Value = "m" }
                    }, "Value", "Text");
                return View(Attendances);
            }catch(Exception ex){
                Console.WriteLine("Error: "+ex.Message);
                ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name");
                ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
                ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name");
                ViewData["DateAndTime"] = DateOnly.FromDateTime(DateTime.Now);
                ViewData["Status"] = new SelectList(new List<SelectListItem> {
                        new SelectListItem { Text = "Present", Value = "1" },
                        new SelectListItem { Text = "Absent", Value = "0" },
                        new SelectListItem { Text = "Excused", Value = "m" }
                    }, "Value", "Text");
                return View(Attendances);
            }

        }

        public async Task<IActionResult> GetLectuerForTeacher(int teacherId)
        {
            var lectuer =_context.Attendances
                .Where(att=> att.DateAndTime == DateOnly.FromDateTime(DateTime.Now))
                .Select(att => att.IdLectuer)
                .FirstOrDefaultAsync();
            var subjects = await _context.TeacherLectuers
                .Where(ts => ts.IdTeacher == teacherId && ts.IdLectuer != lectuer.Result)
                .Include(ts => ts.IdLectuerNavigation)
                .Select(ts => new {
                    id = ts.IdLectuerNavigation.Id,
                    name = ts.IdLectuerNavigation.Name
                }).ToListAsync();
                foreach(var i in subjects){
                    Console.WriteLine($"Lectuer: {i.name}");
                }

            return Json(subjects);
        }

        public async Task<IActionResult> GetClassForSubject(int teacherId, int subjectId)
        {
            var TheClass =_context.Attendances
                .Where(att=> att.DateAndTime == DateOnly.FromDateTime(DateTime.Now))
                .Select(att => att.IdClass)
                .FirstOrDefaultAsync();
            var grades = await _context.TeacherClasses
                .Where(tg => tg.IdTeacher == teacherId && tg.IdClass != TheClass.Result)
                .Include(tg => tg.IdClassNavigation)
                .Select(tg => new {
                    id = tg.IdClassNavigation.Id,
                    name = tg.IdClassNavigation.Name
                }).ToListAsync();
                foreach(var i in grades){
                    Console.WriteLine($"Lectuer: {i.name}");
                }
            return Json(grades);
        }

        public async Task<IActionResult> AttendancesStudent(int studentid)
        {
            //Console.WriteLine($"Student: {studentid}");
            var systemSchoolDbContext =await _context.Attendances.Where(g => g.IdStudent == studentid)
            .Include(g => g.IdLectuerNavigation).Include(g => g.IdStudentNavigation).Include(g => g.IdTeacherNavigation)
            .Include(c => c.IdClassNavigation).ToListAsync();
            return View( systemSchoolDbContext);
        }
        
        // GET: Attendance/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances.FindAsync(id);
            if (attendance == null)
            {
                return NotFound();
            }
            try{
                if (attendance.IdStudent != null)
                {
                    var student = await _context.Students.FindAsync(attendance.IdStudent);
                    if (student == null)
                    {
                        return NotFound("Student not found.");
                    }
                    ViewData["NameStudent"] =student.Name;
                    ViewData["IdLectuer"] =  attendance.IdLectuer;
                    ViewData["IdStudent"] = attendance.IdStudent;
                    ViewData["IdTeacher"] = attendance.IdTeacher;
                    ViewData["DateAndTime"] = attendance.DateAndTime;
                    ViewData["Status"] = new SelectList(new List<SelectListItem> {  
                        new SelectListItem { Text = "Present", Value = "1" },
                        new SelectListItem { Text = "Absent", Value = "0" },
                        new SelectListItem { Text = "Excused", Value = "m" }
                    }, "Value", "Text", attendance.AttendanceStatus);
                    ViewData["Excuse"] = attendance.Excuse;
                    return View(attendance);
                }
                else
                {
                    return NotFound("Student not found.");
                }
                
            }catch(Exception ex){
                //Console.WriteLine("Error: "+ex.Message);
                return NotFound("Error: "+ex.Message);
            }

        }

        // POST: Attendance/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,AttendanceStatus,DateAndTime,Excuse,IdTeacher,IdLectuer,IdStudent")] Attendance attendance)
        {
            if (id != attendance.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(attendance);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AttendanceExists(attendance.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index", new { idTeacher = attendance.IdTeacher });;
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name", attendance.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name", attendance.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name", attendance.IdTeacher);
            return View(attendance);
        }

        // GET: Attendance/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var attendance = await _context.Attendances
                .Include(a => a.IdLectuerNavigation)
                .Include(a => a.IdStudentNavigation)
                .Include(a => a.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (attendance == null)
            {
                return NotFound();
            }

            return View(attendance);
        }

        // POST: Attendance/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var attendance = await _context.Attendances.FindAsync(id);
            
            if (attendance != null)
            {
                int teacher = attendance.IdTeacher??0;
                _context.Attendances.Remove(attendance);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", new { idTeacher = teacher });
            }
            else
            {
                return NotFound("Data is not Found.");
            }

            
        }

        private bool AttendanceExists(int id)
        {
            return _context.Attendances.Any(e => e.Id == id);
        }
    }
}
