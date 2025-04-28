using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    
    public class GradesController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        public GradesController(SystemSchoolDbContext context)
        {
            _context = context;
        }

        // GET: Grades
        [HttpGet]
        [AuthorizeRoles("admin", "Student", "Teacher")]
        public async Task<IActionResult> Index(int teacherId, int studentId)
        {
            var rool = HttpContext.Session.GetString("Role");
            if(rool == "admin")
            {
                var admin = _context.Grades.Where(g => g.IdStudent == studentId).Include(g => g.IdLectuerNavigation).Include(g => g.IdStudentNavigation).Include(g => g.IdTeacherNavigation);
                    return View(await admin.ToListAsync());
            }
            var systemSchoolDbContext = _context.Grades.Where(g => g.IdTeacher == teacherId).Include(g => g.IdLectuerNavigation).Include(g => g.IdStudentNavigation).Include(g => g.IdTeacherNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        // GET: Grades/Details/5
        [HttpGet]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.IdLectuerNavigation)
                .Include(g => g.IdStudentNavigation)
                .Include(g => g.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.GradesId == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }
        [HttpGet]
        // GET: Grades/Create
        [AuthorizeRoles("Teacher")]
        public IActionResult Create(int teacherId, int subjectId, int gradeId)
        {
            // جلب جميع الطلاب المسجلين في الصف والمادة المحددة
            var students = _context.Students
                .Where(student =>
                    student.StudentClasses.Any(sc => sc.IdClass == gradeId) &&
                    student.StudentLectuers.Any(sl => sl.IdLectuer == subjectId) &&
                    _context.TeacherLectuers.Any(tl => tl.IdTeacher == teacherId && tl.IdLectuer == subjectId) &&
                    _context.TeacherClasses.Any(tc => tc.IdTeacher == teacherId && tc.IdClass == gradeId)
                )
                .ToList();

            var studentIds = students.Select(s => (int?)s.Id).ToList();

            var existingGrades = _context.Grades
                .Where(g => g.IdTeacher == teacherId && g.IdLectuer == subjectId && studentIds.Contains(g.IdStudent))
                .Include(g => g.IdStudentNavigation)
                .ToList();

            var studentsWithGrades = students.Select(student =>
            {
                var grade = existingGrades.FirstOrDefault(g => g.IdStudent == student.Id);

                // إذا لم تكن هناك درجة، أنشئ واحدة جديدة بالقيم صفرية
                if (grade == null)
                {
                    grade = new Grade
                    {
                        IdStudent = student.Id,
                        IdTeacher = teacherId,
                        IdLectuer = subjectId,
                        FirstMonth = 0,
                        Mid = 0,
                        SecondMonth = 0,
                        Activity = 0,
                        Final = 0,
                        Total = 0,
                        IdStudentNavigation = student
                    };
                }
                else
                {
                    // ضمان أن القيم الفارغة = 0
                    grade.FirstMonth ??= 0;
                    grade.Mid ??= 0;
                    grade.SecondMonth ??= 0;
                    grade.Activity ??= 0;
                    grade.Final ??= 0;
                    grade.Total ??= 0;
                }

                return new { Student = student, Grade = grade };
                }).ToList();
                var lectuer = _context.Lectuers.FirstOrDefault(l => l.Id == subjectId).Name;
                var Classes = _context.TheClasses.FirstOrDefault(c => c.Id == gradeId).Name;
                ViewBag.StudentsWithGrades = studentsWithGrades;
                ViewBag.SubjectId = subjectId;
                ViewBag.SubjectName = lectuer;
                ViewBag.GradeName = Classes;
                ViewBag.GradeId = gradeId;
                ViewBag.TeacherId = teacherId;

            return View();
        }

        [HttpPost]
        [AuthorizeRoles("Teacher")]
        public IActionResult SaveMark(int studentId, string markType, int markValue, int teacherId, int subjectId)
        {
            // البحث عن السجل إذا كان موجود
            var grade = _context.Grades
                .FirstOrDefault(g => g.IdStudent == studentId && g.IdTeacher == teacherId && g.IdLectuer == subjectId);

            // إذا لم يكن موجودًا، يتم إنشاؤه
            if (grade == null)
            {
                grade = new Grade
                {
                    IdStudent = studentId,
                    IdTeacher = teacherId,
                    IdLectuer = subjectId,
                    FirstMonth = 0,
                    Mid = 0,
                    SecondMonth = 0,
                    Activity = 0,
                    Final = 0
                    // لا تضع Total هنا، لأنه يُحسب تلقائيًا من قاعدة البيانات
                };
                _context.Grades.Add(grade);
            }

            // تعديل القيمة حسب نوع العلامة
            switch (markType)
            {
                case "FirstMonth":
                    grade.FirstMonth = markValue;
                    break;
                case "Mid":
                    grade.Mid = markValue;
                    break;
                case "SecondMonth":
                    grade.SecondMonth = markValue;
                    break;
                case "Activity":
                    grade.Activity = markValue;
                    break;
                case "Final":
                    grade.Final = markValue;
                    break;
            }

            _context.SaveChanges();

            return Ok();
        }
        
        [HttpPost]
        [AuthorizeRoles("Teacher")]
        public IActionResult SaveAll(List<GradeInputViewModel> Grades, int teacherId, int subjectId)
        {
            Console.WriteLine($"F M: {Grades.Count}");
            foreach (var item in Grades)
            {
                Console.WriteLine($"F M: {item.FirstMonth}");
                var grade = _context.Grades
                    .FirstOrDefault(g => g.IdStudent == item.StudentId && g.IdTeacher == teacherId && g.IdLectuer == subjectId);

                if (grade == null)
                {
                    grade = new Grade
                    {
                        IdStudent = item.StudentId,
                        IdTeacher = teacherId,
                        IdLectuer = subjectId
                    };
                    _context.Grades.Add(grade);
                }

                grade.FirstMonth = item.FirstMonth;
                grade.Mid = item.Mid;
                grade.SecondMonth = item.SecondMonth;
                grade.Activity = item.Activity;
                grade.Final = item.Final;
                // Total يُحسب تلقائيًا
            }

            _context.SaveChanges();
            return RedirectToAction("Index", new { teacherId });
        }
        
        [HttpGet]
        [AuthorizeRoles("Student")]
        public async Task<IActionResult> MarkStudent(int studentid)
        {
            var systemSchoolDbContext = _context.Grades.Where(g => g.IdStudent == studentid).Include(g => g.IdLectuerNavigation).Include(g => g.IdStudentNavigation).Include(g => g.IdTeacherNavigation);
            return View(await systemSchoolDbContext.ToListAsync());
        }

        /*public IActionResult Create(int IdTeacher)
        {

            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name");
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name");
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name", IdTeacher);
            ViewData["IdTeac"] = IdTeacher;
            return View();
        }*/
        /*public async Task<IActionResult> Create()
        {
            String email =HttpContext.Session.GetString("Email");
            // احصل على المعرف الحالي للمعلم (يمكن استخدام الجلسة أو المستخدم الحالي)
            var teacherId = _context.Teachers
                .Where(t => t.Email == email)
                .Select(t => t.Id)
                .FirstOrDefault();

            // استرجاع المواد الخاصة بالمعلم
            var subjects = await _context.TeacherLectuers
                .Where(ts => ts.IdTeacher == teacherId)
                .Include(ts => ts.IdLectuerNavigation)
                .Select(ts => new SelectListItem
                {
                    Value = ts.IdLectuerNavigation.Id.ToString(),
                    Text = ts.Id.Name
                })
                .ToListAsync();

            ViewBag.Subjects = subjects;

            return View();
        }
*/
        // POST: Grades/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Create([Bind("GradesId,FirstMonth,Mid,SecondMonth,Activity,Final,Total,IdStudent,IdTeacher,IdLectuer")] Grade grade)
        {
            if (ModelState.IsValid)
            {
                _context.Add(grade);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Name", grade.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Name", grade.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Name", grade.IdTeacher);
            return View(grade);
        }

        // GET: Grades/Edit/5
        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades.FindAsync(id);
            if (grade == null)
            {
                return NotFound();
            }
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", grade.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", grade.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", grade.IdTeacher);
            return View(grade);
        }

        // POST: Grades/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> Edit(int id, [Bind("GradesId,FirstMonth,Mid,SecondMonth,Activity,Final,Total,IdStudent,IdTeacher,IdLectuer")] Grade grade)
        {
            if (id != grade.GradesId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(grade);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!GradeExists(grade.GradesId))
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
            ViewData["IdLectuer"] = new SelectList(_context.Lectuers, "Id", "Id", grade.IdLectuer);
            ViewData["IdStudent"] = new SelectList(_context.Students, "Id", "Id", grade.IdStudent);
            ViewData["IdTeacher"] = new SelectList(_context.Teachers, "Id", "Id", grade.IdTeacher);
            return View(grade);
        }

        // GET: Grades/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var grade = await _context.Grades
                .Include(g => g.IdLectuerNavigation)
                .Include(g => g.IdStudentNavigation)
                .Include(g => g.IdTeacherNavigation)
                .FirstOrDefaultAsync(m => m.GradesId == id);
            if (grade == null)
            {
                return NotFound();
            }

            return View(grade);
        }

        // POST: Grades/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var grade = await _context.Grades.FindAsync(id);
            if (grade != null)
            {
                _context.Grades.Remove(grade);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> GetSubjectsForTeacher(int teacherId)
        {
            var subjects = await _context.TeacherLectuers
                .Where(ts => ts.IdTeacher == teacherId)
                .Include(ts => ts.IdLectuerNavigation)
                .Select(ts => new {
                    id = ts.IdLectuerNavigation.Id,
                    name = ts.IdLectuerNavigation.Name
                }).ToListAsync();

            return Json(subjects);
        }

        [HttpGet]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> GetGradesForSubject(int teacherId, int subjectId)
        {
            var grades = await _context.TeacherClasses
                .Where(tg => tg.IdTeacher == teacherId)
                .Include(tg => tg.IdClassNavigation)
                .Select(tg => new {
                    id = tg.IdClassNavigation.Id,
                    name = tg.IdClassNavigation.Name
                }).ToListAsync();

            return Json(grades);
        }

       [AuthorizeRoles("Teacher")]
        private bool GradeExists(int id)
        {
            return _context.Grades.Any(e => e.GradesId == id);
        }
    }
}
