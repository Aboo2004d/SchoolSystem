using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers
{
    public class TheClassController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        

        public TheClassController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
        }

        /* // GET: TheClass/Details/5
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
         public async Task<IActionResult> Details(Guid? id)
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
         }*/
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }


        // GET: TheClass/Edit/5
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet]
        public async Task<IActionResult> Edit(Guid? id)
        {

            if (id == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TheClass/Edit");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                return RedirectToAction("ManagerMenegarClassView", "Menegar");
            }

            ViewBag.Id = id ?? Guid.Empty;
            return View();
        }

        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> CreateTeacherClass(Guid idClass)
        {
            Guid Id;

            try
            {
                Id = idClass;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Menegar/ManagerMenegarStudentInClassView");
                _notyf.Error("حدث خطأ غير متوقع.");
                return View();
            }

            TheClass? nameClass = _context.TheClasses.Where(lec => lec.Id == Id).FirstOrDefault();
            if (nameClass == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "Lectuer/CreateTeacherLectuer");
                return RedirectToAction("ManagerMenegarClassView");
            }
            
            ViewBag.IdClass = idClass;
            ViewBag.NameLectuer = nameClass.Name;
            List<Teacher> teacher = _context.Teachers.Where(s =>
            _context.TeacherLectuerClasses.Any(t => s.Id == t.IdTeacher && t.IdClass != null && Id != t.IdClass)
            && s.IdSchool == HttpContext.Session.GetGuid("School")).ToList();
            ViewData["IdTeacher"] = new SelectList(teacher, "Id", "Name");
            return View();
        }
        
        private bool TheClassExists(Guid id)
        {
            return _context.TheClasses.Any(e => e.Id == id);
        }
    }
}
