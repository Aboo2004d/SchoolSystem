using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;
using SchoolSystem.Models.AdminSchool;

namespace SchoolSystem.Controllers
{
    [ApiController]
    [Route("/api/lectuer")]
    public class LectuerApiController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;


        public LectuerApiController(SystemSchoolDbContext context, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("GetLectuers")]
        public async Task<IActionResult> GetLectuers(Guid idClass)
        {
            Console.WriteLine($"id: {{idClaas}}");
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "LectuerApi/GetLectuers");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }
            
            Guid Id;

            try
            {
                Id = idClass;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/GetLectuers");
                return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }

            try
            {

                var lectuer = await _context.Lectuers
                    .Where(c => c.IdSchool == school && c.IsDeleted == false &&
                                !_context.TeacherLectuerClasses.Any(l =>
                                l.IdLectuer == c.Id && l.IdClass == Id && l.IsDeletedTeacherLectuerClass == false &&
                                l.IsTeacherRemovedFromClass && l.IsTeacherRemovedFromLectuer)
                    )
                    .Select(c => new
                    {
                        id = c.Id,
                        name = c.Name
                    }).ToListAsync();

                Console.WriteLine("================================================================");
                foreach (var item in lectuer)
                {
                    Console.WriteLine($"Lectuer Name: {item.name}");

                }

                Console.WriteLine("================================================================");


                return Ok(new { success = true, Lectuers = lectuer });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/GetLectuers");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ أثناء جلب الصفوف" });
            }
        }


        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("LectuersData")]
        public async Task<IActionResult> Lectuers(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            try
            {
                // التحقق من صلاحية المستخدم
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "LectuerApi/Lectuers");

                if (!isValid)
                {
                    return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
                }

                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;

                // نعيين قيمة افتراضية لعدد الاعمدة او الحصول على قيمتها
                string orderColumnIndex = "0";
                if (Request.Query.ContainsKey("order[0][column]"))
                {
                    var raw = Request.Query["order[0][column]"].ToString();

                    if (!string.IsNullOrWhiteSpace(raw))
                    {
                        orderColumnIndex = raw;
                    }
                }

                // نعيين قيمة افتراضية لنوع الترتيب او الحصول على قيمتها
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                if (orderDir != "asc" && orderDir != "desc") orderDir = "asc";


                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.Lectuers
                .Where(std => std.IdSchool == school && std.IsDeleted == false)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var AllLectuersInSchool = _context.Lectuers.Where(std => std.IdSchool == school && std.IsDeleted == false)
                    .AsNoTracking()
                    .Select(s => new LectuersInSchool
                    {
                        idLectuer = s.Id,
                        nameLectuer = s.Name,
                        numberOfStudentsInLectuer = s.StudentLectuerTeachers.Where(nsl => nsl.IdSchool == school && nsl.IsDeletedStudent == false).Select(sc => sc.IdStudent).Distinct().Count(),
                        numberOfTeacherInLectuer = s.TeacherLectuerClasses.Where(ntl => ntl.IdSchool == school && ntl.IsDeletedTeacher == false).Select(sc => sc.IdTeacher).Distinct().Count(),
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    AllLectuersInSchool = SearchInDataLectuer(searchValue, AllLectuersInSchool);
                }

                var TotalLectuers = AllLectuersInSchool.Count();

                // الترتيب
                AllLectuersInSchool = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => AllLectuersInSchool.OrderBy(s => s.nameLectuer),
                    ("0", "desc") => AllLectuersInSchool.OrderByDescending(s => s.nameLectuer),
                    ("1", "asc") => AllLectuersInSchool.OrderBy(s => s.numberOfStudentsInLectuer),
                    ("1", "desc") => AllLectuersInSchool.OrderByDescending(s => s.numberOfStudentsInLectuer),
                    ("2", "asc") => AllLectuersInSchool.OrderBy(s => s.numberOfTeacherInLectuer),
                    ("2", "desc") => AllLectuersInSchool.OrderByDescending(s => s.numberOfTeacherInLectuer),
                    _ => AllLectuersInSchool.OrderBy(s => s.nameLectuer)
                };

                // التقطيع (Pagination)
                var Custem_Lectuers = await AllLectuersInSchool
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // الحصول على القيم بعد الفلترة
                var TotalLectuersFilter = Custem_Lectuers.Count();

                // إجمالي عدد السجلات المعروضة في الصفحة
                if (TotalLectuersFilter >= TotalLectuers)
                {
                    TotalLectuersFilter = TotalLectuers;
                }

                // الحصول على البيانات للعرض
                var Lectuers = Custem_Lectuers.
                Select(l => new LectuersInSchool
                {
                    idLectuer = l.idLectuer,
                    nameLectuer = l.nameLectuer,
                    numberOfStudentsInLectuer = l.numberOfStudentsInLectuer,
                    numberOfTeacherInLectuer = l.numberOfTeacherInLectuer
                })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = TotalLectuers,
                    recordsFiltered = TotalLectuersFilter,
                    data = Lectuers
                };
                Console.WriteLine($"Count: {totalRecords}");

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/Lectuers");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطا غير متوقع" });
            }
        }

        private IQueryable<LectuersInSchool> SearchInDataLectuer(string searchValue, IQueryable<LectuersInSchool> AllData)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
                return AllData;

            // السماح فقط بالحروف، الأرقام، المسافات، < > = / .
            searchValue = Regex.Replace(searchValue, @"[^\w\s@.-_]", ""); 

             // البحث النصي على الاسم، الصف، المعدل، الأيام
            return AllData.Where(s =>
                        (s.nameLectuer != null && s.nameLectuer.Contains(searchValue))||
                        (s.numberOfStudentsInLectuer.ToString() != null && s.numberOfStudentsInLectuer.ToString().Contains(searchValue))||
                        (s.numberOfTeacherInLectuer.ToString() != null && s.numberOfTeacherInLectuer.ToString().Contains(searchValue))
                    );
            
        }

        [HttpPost("Create")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostCreateLectuer([FromBody]Lectuer lectuer)
        {
            var school = HttpContext.Session.GetGuid("School") ?? Guid.Empty;
            if (school == Guid.Empty)
            {
                await _logger.LogAsync(new Exception("انتهت صلاحية الدخول"), "TheClassApi/PostCreateLectuer");
                HttpContext.Session.Clear(); // مسح الجلسة
                // تسجيل الخروج باستخدام ملفات تعريف الارتباط
                await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
                return Unauthorized(new { success = false, message = "انتهت صلاحية تسجيل الدخول" });
            }
            try
            {

                if (ModelState.IsValid)
                {
                    if (string.IsNullOrEmpty(lectuer.Name))
                    {
                        return BadRequest(new { success = false, message = "يرجى ادخال اسم المادة" });
                    }
                    lectuer.Name = NormalizeArabic(lectuer.Name);

                    if (_context.Lectuers.Any(c => c.Name == lectuer.Name && c.IdSchool == school))
                    {
                        return Conflict(new { success = false, message = "المادة موجودة مسبقا" });
                    }
                    lectuer.IdSchool = school;
                    lectuer.IsDeleted = false;
                    _context.Add(lectuer);
                    await _context.SaveChangesAsync();
                    return Ok();
                }
                return BadRequest(new { success = false, message = "البيانات  غير صالحة" });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/PostCreateLectuer");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع" });
            }
        }

        [HttpGet("CreateTeacherLectuer")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> GetCreateTeacherInLectuer(Guid idLectuer)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "LectuerApi/PostCreateTeacherInLectuer");
        
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (idLectuer == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "LectuerApi/CreateTeacherInLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = idLectuer;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/CreateTeacherInLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }

            Lectuer? lectuer = await _context.Lectuers
                .Where(l => l.Id == Id && l.IdSchool == school && l.IsDeleted == false).AsNoTracking()
                .FirstOrDefaultAsync();
            if (lectuer == null)
            {
                await _logger.LogAsync(new Exception("تم التلاعب بالمعرف المرسل"), "LectuerApi/CreateTeacherInLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالمعرف المرسل" });
            }

            List<Teacher> teachers = await _context.Teachers.Where(t =>
                    t.IsDeleted == false && t.IdSchool == school&&
                    !_context.TeacherLectuerClasses.Any(tlc => tlc.IdTeacher == t.Id && tlc.IdLectuer == Id &&
                    tlc.IsDeletedTeacher == false && tlc.IsDeletedLectuer == false && tlc.IdSchool == school)
                    ).ToListAsync();

            if (!teachers.Any())
            {
                return BadRequest(new ApiResponse { Success = false, Message = "لا يوجد معلمين مسجلين في المدرسة" });
            }
            var teacherToLectuer = new
            {
                nameLectuer = lectuer.Name,
                teacher = teachers
            };
            return Ok(teacherToLectuer);
        }
        
        [HttpPost("CreateTeacherLectuer")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostCreateTeacherInLectuer([FromBody]LectuerTeacherViewModel teacherLectuer)
        {
           // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "LectuerApi/PostCreateTeacherInLectuer");
        
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (teacherLectuer.IdLectuer == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "LectuerApi/PostCreateTeacherInLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = teacherLectuer.IdLectuer;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/PostCreateTeacherInLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }

            try
            {
                if (ModelState.IsValid)
                {
                    Teacher? teacher = _context.Teachers.FirstOrDefault(t => t.Id == teacherLectuer.IdTeacher);
                    if (teacher == null)
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات المرسلة." });
                    }

                    Lectuer? lectuer = _context.Lectuers.FirstOrDefault(t => t.Id == Id);
                    if (lectuer == null)
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات المرسلة." });
                    }
                    TeacherLectuerClass teacherclass = new TeacherLectuerClass
                    {
                        IdTeacher = teacherLectuer.IdTeacher,
                        IdLectuer = Id,
                        IdSchool = school,
                        IsDeletedTeacher = false,
                        IsDeletedLectuer = false,
                        IsDeletedClass = false,
                        IdClass = null
                    };
                    _context.TeacherLectuerClasses.Add(teacherclass);
                    await _context.SaveChangesAsync();
                    return Ok();
                }
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات المرسلة." });

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/PostCreateTeacherInLectuer");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("Edit")]
        public async Task<IActionResult> GetEditLectuer(Guid? id)
        {
            if (id == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "LectuerApi/GetEditLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/GetEditLectuer");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }

            var lectuer = await _context.Lectuers.Where(l => l.Id == Id && l.IsDeleted ==false).FirstOrDefaultAsync();
            if (lectuer == null)
            {
                return NotFound();
            }
            EditLectuerInSchool lectuers = new EditLectuerInSchool
            {
                idLectuer = id ?? Guid.Empty,
                nameLectuer = lectuer.Name
            };
            return Ok(lectuers);
        }

        
        [HttpPut("Edit")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostEditLectuer(EditLectuerInSchool lectuer)
        {
            if (lectuer.idLectuer == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "LectuerApi/PostEditLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = lectuer.idLectuer;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/PostEditLectuer");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }

            try
            {
                if (ModelState.IsValid)
                {
                    Lectuer? lect = await _context.Lectuers.FirstOrDefaultAsync(c => c.Id == Id);
                    if (lect == null)
                    {
                        await _logger.LogAsync(new Exception("تلاعب بالبيانات"), "LectuerApi/PostEditLectuer");
                        return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات" });
                    }
                    if (lectuer.nameLectuer == null )
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "لا يجب ان يكون اسم المادة فارغ" });
                    }

                    lectuer.nameLectuer = NormalizeArabic(lectuer.nameLectuer);
                    if (_context.Lectuers.Any(l => l.Id != Id && l.Name == lectuer.nameLectuer))
                    {
                        return BadRequest(new ApiResponse { Success = false, Message = "هذه المادة موجودة مسبقا" });
                    }

                    lect.Name = lectuer.nameLectuer;
                    await _context.SaveChangesAsync();

                    return Ok();

                }
                return BadRequest(new ApiResponse { Success = false, Message = "هناك خطأ بالبيانات المدخلة" });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/PostEditLectuer");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("TeacherLectuer")]
        public async Task<IActionResult> TeacherInLectuer(
            Guid idLectuer,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "LectuerApi/TeacherInLectuer");
        
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            Guid Id;
            try
            {
                Id = idLectuer;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/TeacherInLectuer");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
            
            try
            {
                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;
                
                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // الاستعلام الأساسي مع تحسين الأداء
                var AllTeacherInLectuer = _context.TeacherLectuerClasses.Where(std => std.IdSchool == school && std.IsDeletedTeacher == false && std.IdLectuer == Id)
                    .Include(s => s.IdClassNavigation)
                    .Include(s => s.IdTeacherNavigation)
                    .AsNoTracking()
                    .Select(s => new TeacherInLectuerToSchool
                    {
                        idTeacher = s.IdTeacher ?? Guid.Empty,
                        nameClass = s.IdClassNavigation != null && s.IdClassNavigation.IsDeleted == false
                            ? s.IdClassNavigation.Name
                            : (s.IdClassNavigation != null ? s.IdClassNavigation.Name : "") + ("صف محذوف"),
                        nameTeacher = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "UnKnown"


                    });

                int TotalStudentInLectuer = await AllTeacherInLectuer.CountAsync();

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    AllTeacherInLectuer = AllTeacherInLectuer.Where(s =>
                        (s.nameClass != null && s.nameClass.Contains(searchValue)) ||
                        (s.nameTeacher != null && s.nameTeacher.Contains(searchValue))
                    );
                }

                // الحصول على القيم بعد الفلترة
                int TotalFilterStudentInLectuer = await AllTeacherInLectuer.CountAsync();

                // الترتيب
                AllTeacherInLectuer = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => AllTeacherInLectuer.OrderBy(s => s.nameTeacher),
                    ("0", "desc") => AllTeacherInLectuer.OrderByDescending(s => s.nameTeacher),
                    ("1", "asc") => AllTeacherInLectuer.OrderBy(s => s.nameClass),
                    ("1", "desc") => AllTeacherInLectuer.OrderByDescending(s => s.nameClass),
                    _ => AllTeacherInLectuer.OrderBy(s => s.nameTeacher)
                };

                // التقطيع (Pagination)
                var CustmesTeacherInLectuer = await AllTeacherInLectuer
                            .Skip(start)
                            .Take(length)
                            .ToListAsync();

                // ارسال البيانات الى العرض
                var teachersLectuer = CustmesTeacherInLectuer.
                Select(s => new TeacherInLectuerToSchool
                {
                    nameTeacher = s.nameTeacher,
                    nameClass = s.nameClass,
                    idTeacher = s.idTeacher
                }).ToList();

                var result = new
                {
                    draw,
                    recordsTotal = TotalStudentInLectuer,
                    recordsFiltered = TotalFilterStudentInLectuer,
                    data = teachersLectuer
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/TeacherInLectuer");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
        }


        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("StudentLectuer")]
        public async Task<IActionResult> StudentInLectuer(
            Guid idLectuer,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "LectuerApi/StudentInLectuer");
        
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            Guid Id;
            try
            {
                Id = idLectuer;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/StudentInLectuer");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }

            try
            {

                if (length <= 0)
                    length = 10;

                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // الاستعلام الأساسي مع تحسين الأداء
                var AllStudentInLectuer = _context.StudentLectuerTeachers.Where(std => std.IdSchool == school && std.IdLectuer == Id && std.IsDeletedStudent == false)
                    .Include(s => s.IdClassNavigation)
                    .Include(s => s.IdStudentNavigation)
                    .Include(s => s.IdTeacherNavigation)
                    .Include(s => s.IdLectuerNavigation)
                    .AsNoTracking()
                    .Select(s => new StudentInLectuerToSchool
                    {
                        nameStudent = s.IdStudentNavigation == null ? "غير معروف" :
                            s.IdStudentNavigation.IsDeletedStudent ? s.IdStudentNavigation.Name + "(طالب محذوف)" : s.IdStudentNavigation.Name,
                        nameClass = s.IdClassNavigation == null ? "غير معروف" :
                            s.IdClassNavigation.IsDeleted ? s.IdClassNavigation.Name + "(صف محذوف)" : s.IdClassNavigation.Name,
                        nameTeacher = s.IdTeacherNavigation == null ? "غير معروف" :
                            s.IdTeacherNavigation.IsDeleted ? s.IdTeacherNavigation.Name + "(المعلم لا يدرس الصف)" : s.IdTeacherNavigation.Name,
                        grade = ((_context.Grades.Where(g => g.IdSchool == school && g.IdStudent == s.IdStudent &&
                            g.IdClass == s.IdClass && g.IdTeacher == s.IdTeacher && g.IdLectuer == s.IdLectuer)
                            .Select(g => g.Total).FirstOrDefault() ?? 0) + "/100")
                    });

                int TotalStudentInLectuer = await AllStudentInLectuer.CountAsync();

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    AllStudentInLectuer = SearchInDataStudentInLectuer(searchValue, AllStudentInLectuer);
                }

                // الحصول على القيم بعد الفلترة
                int TotalFilterStudentInLectuer = await AllStudentInLectuer.CountAsync();

                // الترتيب
                AllStudentInLectuer = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => AllStudentInLectuer.OrderBy(s => s.nameStudent),
                    ("0", "desc") => AllStudentInLectuer.OrderByDescending(s => s.nameStudent),
                    ("1", "asc") => AllStudentInLectuer.OrderBy(s => s.nameClass),
                    ("1", "desc") => AllStudentInLectuer.OrderByDescending(s => s.nameClass),
                    ("2", "asc") => AllStudentInLectuer.OrderBy(s => s.nameTeacher),
                    ("2", "desc") => AllStudentInLectuer.OrderByDescending(s => s.nameTeacher),
                    ("3", "asc") => AllStudentInLectuer.OrderBy(s => s.grade),
                    ("3", "desc") => AllStudentInLectuer.OrderByDescending(s => s.grade),
                    _ => AllStudentInLectuer.OrderBy(s => s.nameStudent)
                };

                // تقطيع
                var CustmesStudentInLectuer = await AllStudentInLectuer
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();
                
                // الحصول على البيانات للعرض
                var studentsLectuer = CustmesStudentInLectuer.
                Select(s => new StudentInLectuerToSchool
                {
                    nameTeacher = s.nameTeacher,
                    nameClass = s.nameClass,
                    nameStudent = s.nameStudent,
                    grade = s.grade
                }).ToList();

                var result = new
                {
                    draw,
                    recordsTotal = TotalStudentInLectuer,
                    recordsFiltered = TotalFilterStudentInLectuer,
                    data = studentsLectuer
                };
                
                return Ok(result);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/StudentInLectuer");
                return StatusCode(500,new ApiResponse { Success = false, Message =  "حدث خطأ غير متوقع " });
            }
        }

        private IQueryable<StudentInLectuerToSchool> SearchInDataStudentInLectuer(string searchValue, IQueryable<StudentInLectuerToSchool> AllData)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
                return AllData;

           // السماح فقط بالحروف، الأرقام، المسافات، < > = / .
            searchValue = Regex.Replace(searchValue, @"[^\w\s<>=/\.]", ""); 

            // محاولة استخراج المقارنة العددية (المعدل)
            var match = Regex.Match(searchValue, @"(?<operator><=|>=|<|>|=)\s*(?<number>\d+(\.\d+)?)");
            if (match.Success)
            {
                string operatorSymbol = match.Groups["operator"].Value;
                string numberStr = match.Groups["number"].Value;

                // التأكد من أن الرقم صالح للتحويل
                if (!double.TryParse(numberStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double number))
                {
                    Console.WriteLine($"Invalid number after operator: '{numberStr}'");
                    return AllData; // إعادة كل البيانات إذا الرقم غير صالح
                }

                // تحميل بيانات AllData في الذاكرة بعد تحويلها إلى List
                var listData = AllData.ToList();
                Console.WriteLine($"Count All Data: {listData.Count()}");

                listData = listData
                    .Where(std =>
                    {

                        if (string.IsNullOrWhiteSpace(std.grade))
                            return false;

                        // تنظيف النص وإزالة أي % وفراغات
                        string avgStr = std.grade.Replace("/100", "").Trim();

                        // تحويل الرقم بشكل موثوق
                        if (!double.TryParse(avgStr, NumberStyles.Any, CultureInfo.InvariantCulture, out double avg))
                        {
                            return false;
                        }

                        return operatorSymbol switch
                        {
                            "<" => avg < number,
                            ">" => avg > number,
                            "<=" => avg <= number,
                            ">=" => avg >= number,
                            "=" => avg == number,
                            _ => true
                        };
                    }).ToList();

                return listData.AsQueryable();
            }
            else
            {
                 // البحث النصي 
                return AllData.Where(s =>
                    (s.nameClass != null && s.nameClass.Contains(searchValue))||
                    (s.nameStudent != null && s.nameStudent.Contains(searchValue))||
                    (s.grade != null && s.grade.Contains(searchValue))||
                    (s.nameTeacher != null && s.nameTeacher.Contains(searchValue))
                );
            }

            
            
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpDelete("Delete")]
        public async Task<IActionResult> DeleteLectuer([FromBody] DeleteInSchool deleteInSchool)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "LectuerApi/DeleteLectuer");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (deleteInSchool.id == null)
            {
                await _logger.LogAsync(new Exception("معرف فارغ"), "LectuerApi/DeleteLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = deleteInSchool.id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/DeleteLectuer");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
            try
            {
                var lectuer = await _context.Lectuers.FindAsync(Id);

                if (lectuer == null)
                {
                    await _logger.LogAsync(new Exception("تلاعب بالبيانات"), "LectuerApi/DeleteLectuer");
                    return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات" });
                }

                lectuer.IsDeleted = true;

                List<Attendance> attendances = await _context.Attendances.Where(att => att.IdLectuer == Id && att.IsDeletedLectuer == false && att.IdSchool == school).ToListAsync();
                foreach (var attendance in attendances)
                {
                    attendance.IsDeletedLectuer = true;
                }

                List<TeacherLectuerClass> teacherLectuerClasses = await _context.TeacherLectuerClasses.Where(tlc => tlc.IdLectuer == Id && tlc.IsDeletedLectuer == false && tlc.IdSchool == school).ToListAsync();
                foreach (var teacherLectuerClass in teacherLectuerClasses)
                {
                    teacherLectuerClass.IsDeletedLectuer = true;
                }

                List<StudentLectuerTeacher> studentLectuerTeachers = await _context.StudentLectuerTeachers.Where(tlc => tlc.IdLectuer == Id && tlc.IsDeletedLectuer == false && tlc.IdSchool == school).ToListAsync();
                foreach (var teacherLectuerClass in studentLectuerTeachers)
                {
                    teacherLectuerClass.IsDeletedLectuer = true;
                }

                List<Grade> grades = await _context.Grades.Where(g => g.IdLectuer == Id && g.IsDeletedLectuer == false && g.IdSchool == school).ToListAsync();
                foreach (var grade in grades)
                {
                    grade.IsDeletedLectuer = true;
                }

                await _context.SaveChangesAsync();
                return Ok();

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/DeleteLectuer");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }

        }
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpDelete("DeleteTeacher")]
        public async Task<IActionResult> DeleteTeacherLectuer([FromBody] DeleteInSchool deleteInSchool)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "LectuerApi/DeleteTeacherLectuer");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (deleteInSchool.id == null)
            {
                await _logger.LogAsync(new Exception("معرف فارغ"), "LectuerApi/DeleteTeacherLectuer");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = deleteInSchool.id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/DeleteTeacherLectuer");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
            try
            {
                Teacher? teacher = await _context.Teachers.Where(teach => teach.Id == Id && teach.IdSchool == school && teach.IsDeleted == false).FirstOrDefaultAsync();
                if (teacher == null)
                {
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "LectuerApi/DeleteTeacherLectuer");
                    return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات المرسلة" });
                }
                
                List<TeacherLectuerClass> teacherLectuerClass = await _context.TeacherLectuerClasses.Where(tlc => tlc.IdTeacher == Id && tlc.IsTeacherRemovedFromLectuer == false && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
                foreach (var teachers in teacherLectuerClass)
                {
                    teachers.IsTeacherRemovedFromLectuer = true;
                }

                List<StudentLectuerTeacher> studentLectuerTeachers = await _context.StudentLectuerTeachers.Where(tlc => tlc.IdTeacher == Id && tlc.IsTeacherRemovedFromLectuer == false && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
                foreach (var teachers in studentLectuerTeachers)
                {
                    teachers.IsTeacherRemovedFromLectuer = true;
                }

                List<Grade> grade = await _context.Grades.Where(tlc => tlc.IdTeacher == Id && tlc.IsTeacherRemovedFromLectuer == false && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
                foreach (var teachers in grade)
                {
                    teachers.IsTeacherRemovedFromLectuer = true;
                }

                List<Attendance> attendances = await _context.Attendances.Where(tlc => tlc.IdTeacher == Id && tlc.IsTeacherRemovedFromLectuer == false && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
                foreach (var teachers in attendances)
                {
                    teachers.IsTeacherRemovedFromLectuer = true;
                }

                await _context.SaveChangesAsync();
                return Ok();

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "LectuerApi/DeleteTeacherLectuer");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }

        }

    public static string NormalizeArabic(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;

            string normalized = input.Trim();
            normalized = Regex.Replace(normalized, @"\s+", " ");

            normalized = normalized.Replace("أ", "ا")
                                   .Replace("إ", "ا")
                                   .Replace("آ", "ا");

            normalized = normalized.Replace("ى", "ي");

            normalized = normalized.Replace("ة", "ه");

            normalized = Regex.Replace(normalized, "[ًٌٍَُِّْ]", "");

            return normalized.ToLower();
        }

        

    }
}
