using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;
using SchoolSystem.Models.AdminSchool;

namespace SchoolSystem.Controllers
{
    [ApiController]
    [Route("api/theClass")]
    public class TheClassApiController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;
        private readonly IDistributedCache _cache;


        public TheClassApiController(SystemSchoolDbContext context, IDistributedCache cache, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
            _cache = cache;
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("GetClasses")]
        public async Task<IActionResult> GetClassesToTeacher(Guid idTeacher)
        {
            Console.WriteLine("================================123================================");
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/GetClassesToTeacher");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            Guid Id;
            try
            {
                Id = idTeacher;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/GetClassesToTeacher");
                return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }

            try
            {

                var classes = await _context.TheClasses.Where(c => 
                            c.IdSchool == school && c.IsDeleted == false &&
                            !_context.TeacherLectuerClasses.Any(t =>
                                t.IdClass == c.Id &&
                                t.IdTeacher == Id && t.IdSchool == school &&
                                t.IsTeacherRemovedFromClass == false && t.IsTeacherRemovedFromLectuer == false)
                    ).Select(c => new
                    {
                        id = c.Id,
                        name = c.Name
                    }).ToListAsync();

                var lastClassesToLectuer = await _context.TeacherLectuerClasses.Where(t =>
                                t.IdTeacher == Id && t.IdSchool == school &&
                                t.IsTeacherRemovedFromClass == false && t.IsTeacherRemovedFromLectuer == false
                ).Select(teacher => new
                {
                    idTeacher = teacher.IdTeacher ?? Guid.Empty,
                    teacherName = teacher.IdTeacherNavigation.Name,
                    nameLectuer = teacher.IdLectuerNavigation.Name,
                    idLectuer = teacher.IdLectuer ?? Guid.Empty,
                    nameClass = teacher.IdClassNavigation.Name,
                    idClass = teacher.IdClass ?? Guid.Empty,
                }).ToListAsync();

                return Ok(new { success = true, theClasses = classes, lastClassesToTeacher = lastClassesToLectuer });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/GetClassesToTeacher");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب الصفوف" });
            }
        }
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("GetClassToStudent")]
        public async Task<IActionResult> GetClassesPerStudent()
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/GetClassesPerStudent");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

           try
            {

                var classes = await _context.TheClasses.Where(c => 
                            c.IdSchool == school && c.IsDeleted == false 
                    ).Select(c => new GetClassInSchool
                    {
                        idClass = c.Id,
                        nameClass = c.Name
                    }).ToListAsync();

                return Ok(new { success = true, theClasses = classes});
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/GetClassesPerStudent");
                return StatusCode(500, new { success = false, message = "حدث خطأ أثناء جلب الصفوف" });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("Create")]
        public async Task<ActionResult<GetCreateClass>> GetCreateClass()
        {

            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/GetCreateClass");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            try
            {
                TheClass? theClass = await _context.TheClasses.Where(c => c.IdSchool == school && c.IsDeleted == false)
                    .FirstOrDefaultAsync();
                if (theClass == null)
                {
                    await _logger.LogAsync(new Exception("تم التلاعب بالبيانات"), "TheClassApi/GetCreateClass");
                    return BadRequest(new { success = false, message = "تم التلاعب بالبيانات" });
                }
                School? school1 = await _context.Schools.Where(s => s.Id == school && s.IsDeleted == false).Include(s => s.IdStageNavigation)
                    .FirstOrDefaultAsync();
                if (school1 == null)
                {
                    await _logger.LogAsync(new Exception("تم التلاعب بالبيانات"), "TheClassApi/GetCreateClass");
                    return BadRequest(new { success = false, message = "تم التلاعب بالبيانات" });
                }

                if (school1.IdStageNavigation.Code.ToLower() == "c")
                {

                    List<BranchClass> branch = await _context.Branches.Select(s => new BranchClass
                    {
                        idBranch = s.Id,
                        nameBranch = s.BranchName
                    }).ToListAsync();

                    GetCreateClass theClassViewModel = new GetCreateClass
                    {
                        IsBranche = true,
                        Branches = branch
                    };
                    return Ok(theClassViewModel);
                }
                else
                {
                    GetCreateClass theClassViewModel = new GetCreateClass
                    {
                        IsBranche = false,
                    };
                    return Ok(theClassViewModel);
                }

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/GetCreateClass");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع." });
            }
        }


        [HttpPost("Create")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostCreateClass([FromBody]PostCreateClass theClass)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/PostCreateClass");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            try
            {
                if (ModelState.IsValid)
                {
                    
                    if (theClass == null || string.IsNullOrWhiteSpace(theClass.nameClass))
                    {
                        await _logger.LogAsync(new Exception("الصف فارغ"), "TheClassApi/PostCreateClass");
                        return BadRequest(new { success = false, message = "لا يمكن ارسال بيانات ناقصة الى التحقق" });
                    }

                    theClass.nameClass = NormalizeArabic(theClass.nameClass);

                    if (_context.TheClasses.Any(nc => nc.Name == theClass.nameClass))
                    {
                        return Conflict(new { success = false, message = "الصف موجود مسبقا" });
                    }

                    School? school1 = await _context.Schools.Where(s => s.Id == school && s.IsDeleted == false).Include(s => s.IdStageNavigation)
                    .FirstOrDefaultAsync();
                    if (school1 == null)
                    {
                        await _logger.LogAsync(new Exception("تم التلاعب بالبيانات"), "TheClassApi/PostCreateClass");
                        return BadRequest(new { success = false, message = "تم التلاعب بالبيانات" });
                    }

                    if (school1.IdStageNavigation.Code.ToLower() == "c")
                    {
                        if (theClass.idBranch == null)
                        {

                            return BadRequest(new { success = false, message = "يجب اختيار فرع للصف" });
                        }

                        Guid Id;
                        try
                        {
                            Id = theClass.idBranch ?? Guid.Empty;

                        }
                        catch (Exception ex)
                        {
                            await _logger.LogAsync(ex, "TheClassApi/PostCreateClass");
                            return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
                        }

                        Branch? branch = await _context.Branches.Where(b => b.Id == Id).FirstOrDefaultAsync();
                        if (branch == null)
                        {
                            await _logger.LogAsync(new Exception("تم التلاعب بالبيانات"), "TheClassApi/PostCreateClass");
                            return BadRequest(new { success = false, message = "تم التلاعب بالبيانات" });
                        }
                        Console.WriteLine("================================123================================");
                        Console.WriteLine($"Class Name: {theClass.nameClass}");
                        Console.WriteLine($"Class I Branchd: {Id}");
                        Console.WriteLine($"Class Section: {theClass.section}");
                        Console.WriteLine($"Class Number: {theClass.numberClass}");
                        Console.WriteLine("================================123================================");


                        TheClass theClass1 = new TheClass
                        {
                            Name = theClass.nameClass,
                            NumberClass = theClass.numberClass,
                            Section = theClass.section,
                            IdBranch = Id,
                            IdStage = school1.IdStage,
                            IdSchool = school,
                            IsDeleted = false,
                            IsDeletedSchool = false
                        };
                        _context.TheClasses.Add(theClass1);
                    }
                    else
                    {
                        TheClass theClass1 = new TheClass
                        {
                            Name = theClass.nameClass,
                            NumberClass = theClass.numberClass,
                            Section = theClass.section,
                            IdBranch = null,
                            IdStage = school1.IdStage,
                            IdSchool = school,
                            IsDeleted = false,
                            IsDeletedSchool = false
                        };
                        _context.TheClasses.Add(theClass1);
                    }

                    await _context.SaveChangesAsync();

                    return Ok();
                }
                return BadRequest(new { success = false, message = "البيانات  غير صالحة" });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/PostCreateClass");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع" });
            }
            
        }


        // GET: TheClass/Edit/5
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("Edit")]
        public async Task<ActionResult<EditClassInSchool>> GetEditClass(Guid? id)
        {

            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/GetEditClass");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (id == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "TheClassApi/GetEditClass");
                return BadRequest(new { success = false, message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/GetEditClass");
                return BadRequest(new { success = false, message = "حدث خطأ غير متوقع." });
            }

            try
            {
                TheClass? theClass = await _context.TheClasses.Where(c => c.Id == Id && c.IdSchool == school && c.IsDeleted == false)
                    .FirstOrDefaultAsync();
                if (theClass == null)
                {
                    await _logger.LogAsync(new Exception("تم التلاعب بالمعرف المرسل"), "TheClassApi/GetEditClass");
                    return BadRequest(new { success = false, message = "تم التلاعب بالمعرف المرسل" });
                }

                EditClassInSchool theClassViewModel = new EditClassInSchool
                {
                    idClass = theClass.Id,
                    nameClass = theClass.Name
                };
                return Ok(theClassViewModel);

            }
            catch(Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/GetEditClass");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع." });
            }
        }

        [HttpPut("Edit")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostEditClass([FromBody]EditClassInSchool theClass)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/GetEditClass");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (theClass.idClass == null)
            {
                await _logger.LogAsync(new Exception("معرف فارغ"), "TheClassApi/PostEditClass");
                return BadRequest(new { success = false, message = "لا يمكن ارسال بيانات ناقصة الى التحقق" });
            }

            Guid Id;

            try
            {
                Id = theClass.idClass ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/PostEditClass");
                return StatusCode(500,new { success = false, message = "حدث خطأ غير متوقع" });
            }

            try
            {
                if (ModelState.IsValid)
                {
                    if(theClass.nameClass == null || theClass.nameClass.Trim() == "")
                    {
                        await _logger.LogAsync(new Exception("اسم الصف فارغ"), "TheClassApi/PostEditClass");
                        return BadRequest(new { success = false, message = "لا يمكن ارسال اسم صف فارغ" });
                    }

                    theClass.nameClass = NormalizeArabic(theClass.nameClass);
                    if(_context.TheClasses.Any(c => c.Name == theClass.nameClass && c.Id != Id && c.IdSchool == school && c.IsDeleted == false))
                    {
                        return Conflict(new { success = false, message = "اسم الصف موجود بالفعل" });
                    }

                    TheClass? theClass1 = await _context.TheClasses.FirstOrDefaultAsync(c => c.Id == Id && c.IdSchool == school && c.IsDeleted == false);
                    if (theClass1 == null)
                    {
                        await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TheClassApi/PostEditClass");

                        return BadRequest(new { success = false, message = "لا يمكن التلاعب بالبيانات المرسلة" });
                    }

                    if (_context.TheClasses.Any(c => c.Name == theClass.nameClass && c.Id != Id && c.IdSchool == school && c.IsDeleted == false))
                    {
                        return Conflict(new { success = false, message = "اسم الصف موجود بالفعل" });
                    }

                    theClass1.Name = theClass.nameClass;
                    await _context.SaveChangesAsync();
                    string studentsKey = $"Students_School_{school}";
                    await _cache.RemoveAsync(studentsKey);
                    return Ok();
                }
                return BadRequest(new { success = false, message = "هناك خطأ بالبيانات المدخلة" });
            }
            catch (Exception ex)
            {
                
                await _logger.LogAsync(ex, "TheClassApi/PostEditClass");
                return StatusCode(500,new{success = false, message = "حدث خطأ غير متوقع"});
            }

        }

        [HttpGet("CreateTeacherClass")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> GetCreateTeacherClass(Guid idClass)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/GetCreateTeacherClass");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (idClass == null)
            {
                await _logger.LogAsync(new Exception("معرف فارغ"), "TheClassApi/GetCreateTeacherClass");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = idClass;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/GetCreateTeacherClass");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }

            try
            {

                TheClass? theClass =await _context.TheClasses.Where(classes => classes.Id == Id &&
                classes.IdSchool == school && classes.IsDeleted == false ).FirstOrDefaultAsync();
                
                if (theClass == null)
                {
                    await _logger.LogAsync(new Exception("تلاعب بالبيانات المرسلة"), "TheClassApi/GetCreateTeacherClass");
                    return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات المرسلة" });
                }

                List<Teachers> teachers =await _context.Teachers.Where(teacher =>
                teacher.IsDeleted == false && teacher.IdSchool == school &&
                !_context.TeacherLectuerClasses.Any(teacherclasslectuer =>
                teacherclasslectuer.IdSchool == school && teacherclasslectuer.IsDeletedTeacher == false &&
                teacherclasslectuer.IdTeacher == teacher.Id  && teacherclasslectuer.IdClass == Id)
                ).Select(teacher => new Teachers
                {
                    idTeacher = teacher.Id,
                    nameTeacher = teacher.Name??"غير معرف"
                }).ToListAsync();

                GetCreateTeacherToClass getCreateTeacherToClass = new GetCreateTeacherToClass
                {
                    idClass = theClass.Id,
                    nameClass = theClass.Name,
                    teachers = teachers,
                    lectuers = await _context.Lectuers.Where(l => l.IdSchool == school && l.IsDeleted == false)
                    .Select(l => new Lectuers
                    {
                        idLectuer = l.Id,
                        nameLectuer = l.Name??"غير معرف"
                    }).ToListAsync()
                };

                return Ok(getCreateTeacherToClass);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/GetCreateTeacherClass");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
        }

        /*[HttpPost("CreateTeacherClass")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostCreateTeacherClass([FromBody] PostCreateTeacherToClass teacherClass)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/GetCreateTeacherClass");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (teacherClass.idClass == null)
            {
                await _logger.LogAsync(new Exception("معرف فارغ"), "TheClassApi/PostCreateTeacherClass");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid idClass;
            Guid idTeacher;

            try
            {
                idClass = teacherClass.idClass;
                idTeacher = teacherClass.idTeacher;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/PostCreateTeacherClass");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
            try
            {
                if (ModelState.IsValid)
                {

                    Teacher? teacher = await _context.Teachers.AsNoTracking().FirstOrDefaultAsync(t => t.Id == idTeacher && t.IdSchool == school && t.IsDeleted == false);
                    if (teacher != null)
                    {
                        TheClass? theClass = await _context.TheClasses.AsNoTracking().FirstOrDefaultAsync(t => t.Id == idClass && t.IdSchool == school && t.IsDeleted == false);
                        if (theClass != null)
                        {

                            TeacherLectuerClass? teacherLectuer = await _context.TeacherLectuerClasses
                            .Where(tlc =>
                            tlc.IdSchool == school && tlc.IdTeacher == idTeacher
                            ).AsNoTracking().FirstOrDefaultAsync();
                            if (teacherLectuer == null)
                            {
                                return BadRequest(new { success = false, message = "يجب تسجيل المادة للمعلم اولا" });
                            }

                            TeacherLectuerClass? teacherLectuerClass = await _context.TeacherLectuerClasses
                            .Where(tlc =>
                            tlc.IdSchool == school && tlc.IdTeacher == idTeacher
                            && tlc.IdClass == null
                            ).AsTracking().FirstOrDefaultAsync();
                            if (teacherLectuerClass != null)
                            {
                                TeacherLectuerClass? otherTeacherLectuerClass = await _context.TeacherLectuerClasses
                                    .Where(tlc =>
                                    tlc.IdSchool == school && tlc.IdTeacher != idTeacher
                                    && tlc.IdLectuer == teacherLectuer.IdLectuer
                                    && tlc.IdClass == idClass && tlc.IsDeletedClass == false
                                    ).AsNoTracking().FirstOrDefaultAsync();
                                if (otherTeacherLectuerClass != null)
                                {
                                    return Conflict(new { success = false, message = "هناك معلم اخر يدرس المادة في هذا الصف" });
                                }
                                else
                                {
                                    teacherLectuerClass.IdClass = idClass;
                                    teacherLectuerClass.IdSchool = school;
                                    teacherLectuerClass.IsDeletedClass = false;
                                    await _context.SaveChangesAsync();

                                    List<StudentLectuerTeacher>? studentLectuerTeacher = await _context.StudentLectuerTeachers
                                        .Where(slt => slt.IdSchool == school && slt.IdLectuer == teacherLectuer.IdLectuer && slt.IdClass == idClass && slt.IsDeletedClass == false)
                                        .AsTracking().ToListAsync();
                                    if (studentLectuerTeacher.Any())
                                    {
                                        foreach (var item in studentLectuerTeacher)
                                        {
                                            item.IdTeacher = idTeacher;
                                            _context.StudentLectuerTeachers.Update(item);
                                        }
                                        await _context.SaveChangesAsync();
                                        return Ok();
                                    }
                                    else
                                    {
                                        List<Student>? student = await _context.Students
                                            .Where(s => s.IdSchool == school && s.IdClass == idClass && s.IsDeletedClass == false)
                                            .AsNoTracking().ToListAsync();
                                        if (student.Any())
                                        {
                                            foreach (var std in student)
                                            {
                                                StudentLectuerTeacher students = new StudentLectuerTeacher
                                                {
                                                    IdSchool = school,
                                                    IdLectuer = teacherLectuer.IdLectuer,
                                                    IdClass = idClass,
                                                    IdTeacher = idTeacher,
                                                    IsDeletedClass = false,
                                                    IsDeletedLectuer = false,
                                                    IsDeletedStudent = false,
                                                    IsDeletedTeacher = false,
                                                    IsDeletedStudentLectuerTeacher = false,
                                                    IdStudent = std.Id
                                                };
                                                _context.StudentLectuerTeachers.Add(students);

                                            }
                                            await _context.SaveChangesAsync();
                                            
                                        }
                                        return Ok();
                                    }
                                }

                            }
                            else
                            {
                                TeacherLectuerClass? teacherLectuerClass1 = await _context.TeacherLectuerClasses
                                    .Where(tlc =>
                                    tlc.IdSchool == school && tlc.IdTeacher == idTeacher
                                    && tlc.IdLectuer == teacherLectuer.IdLectuer
                                    && tlc.IdClass == teacherLectuer.IdClass && tlc.IsDeletedClass == false
                                    ).AsNoTracking().FirstOrDefaultAsync();
                                if (teacherLectuerClass1 == null)
                                {
                                    TeacherLectuerClass? teacherLectuerClass2 = await _context.TeacherLectuerClasses
                                        .Where(tlc =>
                                        tlc.IdSchool == school && tlc.IdTeacher != idTeacher
                                        && tlc.IdLectuer == teacherLectuer.IdLectuer
                                        && tlc.IdClass == teacherLectuer.IdClass && tlc.IsDeletedClass == false
                                        ).AsNoTracking().FirstOrDefaultAsync();
                                    if (teacherLectuerClass2 == null)
                                    {
                                        TeacherLectuerClass newTeacherLectuerClass = new TeacherLectuerClass
                                        {
                                            IdSchool = school,
                                            IdTeacher = idTeacher,
                                            IdLectuer = teacherLectuer.IdLectuer,
                                            IdClass = idClass,
                                            IsDeletedClass = false
                                        };
                                        _context.TeacherLectuerClasses.Add(newTeacherLectuerClass);
                                        await _context.SaveChangesAsync();

                                        List<Student>? students1 = await _context.Students
                                            .Where(s => s.IdSchool == school && s.IdClass == idClass && s.IsDeletedClass == false)
                                            .AsNoTracking().ToListAsync();
                                        if (students1.Any())
                                        {
                                            foreach (var std in students1)
                                            {
                                                StudentLectuerTeacher students = new StudentLectuerTeacher
                                                {
                                                    IdSchool = school,
                                                    IdLectuer = teacherLectuer.IdLectuer,
                                                    IdClass = idClass,
                                                    IdTeacher = idTeacher,
                                                    IsDeletedClass = false,
                                                    IsDeletedLectuer = false,
                                                    IsDeletedStudent = false,
                                                    IsDeletedTeacher = false,
                                                    IsDeletedStudentLectuerTeacher = false,
                                                    IdStudent = std.Id
                                                };
                                                _context.StudentLectuerTeachers.Add(students);

                                            }
                                            await _context.SaveChangesAsync();
                                            
                                        }
                                        return Ok();
                                    }
                                    else
                                    {
                                        return Conflict(new { success = false, message = "هناك معلم اخر يدرس نفس المادة في الصف" });
                                    }
                                }
                                else
                                {
                                    TeacherLectuerClass? teacherLectuerClass3 = await _context.TeacherLectuerClasses
                                    .Where(tlc =>
                                    tlc.IdSchool == school && tlc.IdTeacher == idTeacher
                                    && tlc.IdLectuer == teacherLectuer.IdLectuer
                                    && tlc.IdClass != teacherLectuer.IdClass && tlc.IsDeletedClass == false
                                    ).AsNoTracking().FirstOrDefaultAsync();

                                    if (teacherLectuerClass3 == null)
                                    {
                                        return Conflict(new { success = false, message = "هناك معلم اخر يدرس نفس المادة في الصف" });
                                    }
                                    else
                                    {
                                        
                                        return Conflict(new { success = false, message = "تم اضافة المعلم لهذا الصف بنفس المادة مسبقا" });
                                    }

                                }
                            }

                        }
                        else
                        {
                            await _logger.LogAsync(new Exception("تلاعب بمعرف الصف المرسل"), "TheClassApi/PostCreateTeacherClass");
                            return BadRequest(new { success = false, message = "لا يمكن التلاعب بالمعرف المرسل" });
                        }
                    }
                    else
                    {
                        await _logger.LogAsync(new Exception("تلاعب بمعرف المعلم المرسلة"), "TheClassApi/PostCreateTeacherClass");
                        return BadRequest(new { success = false, message = "لا يمكن التلاعب بالبيانات المرسلة" });
                    }
                }
                else
                {
                    return BadRequest(new { success = false, message = "البيانات المدخلة خاطئة" });
                }
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/PostCreateTeacherClass");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع." });
            }
        }
*/
        
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpDelete("Delete")]
        public async Task<IActionResult> DeleteClass([FromBody] DeleteInSchool deleteInSchool)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TheClassApi/DeleteClass");

            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (deleteInSchool.id == null)
            {
                await _logger.LogAsync(new Exception("معرف فارغ"), "TheClassApi/DeleteClass");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = deleteInSchool.id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/DeleteClass");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
            try
            {
                var theClass = await _context.TheClasses.FindAsync(Id);

                if (theClass == null)
                {
                    await _logger.LogAsync(new Exception("تلاعب بالبيانات"), "TheClassApi/DeleteClass");
                    return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات" });
                }

                theClass.IsDeleted = true;

                List<Attendance> attendances = await _context.Attendances.Where(att => att.IdClass == Id && att.IsDeletedClass == false && att.IdSchool == school).ToListAsync();
                foreach (var attendance in attendances)
                {
                    attendance.IsDeletedClass = true;
                }

                List<Student> students = await _context.Students.Where(std => std.IdClass == Id && std.IsDeletedClass == false && std.IdSchool == school).ToListAsync();
                foreach (var student in students)
                {
                    student.IsDeletedClass = true;
                }

                List<TeacherLectuerClass> teacherLectuerClasses = await _context.TeacherLectuerClasses.Where(tlc => tlc.IdClass == Id && tlc.IsDeletedClass == false && tlc.IdSchool == school).ToListAsync();
                foreach (var teacherLectuerClass in teacherLectuerClasses)
                {
                    teacherLectuerClass.IsDeletedClass = true;
                }

                List<Grade> grades = await _context.Grades.Where(g => g.IdClass == Id && g.IsDeletedClass == false && g.IdSchool == school).ToListAsync();
                foreach (var grade in grades)
                {
                    grade.IsDeletedClass = true;
                }

                await _context.SaveChangesAsync();
                string studentsKey = $"Students_School_{school}";
                await _cache.RemoveAsync(studentsKey);
                return Ok();

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TheClassApi/DeleteClass");
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
        private bool TheClassExists(Guid id)
        {
            return _context.TheClasses.Any(e => e.Id == id);
        }
    }
}
