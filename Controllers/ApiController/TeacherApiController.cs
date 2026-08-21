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
using Newtonsoft.Json;
using NuGet.Packaging.Signing;
using QuestPDF.Fluent;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;
using SchoolSystem.Models.AdminSchool;

namespace SchoolSystem.Controllers
{
    [ApiController]
    [Route("api/teacher")]
    public class TeacherApiController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;
        private readonly IEmailValidationService _emailValidator;
        private readonly IDistributedCache _cache;


        public TeacherApiController(SystemSchoolDbContext context, IDistributedCache cache, IEmailValidationService emailValidator, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
            _emailValidator = emailValidator;
            _cache = cache;
        }

        [HttpPost("Create")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostCreateTeacher([FromBody] Teacher teacher)
        {
            try
            {
                // التحقق من صلاحية المستخدم
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TeacherApi/PostCreateTeacher");
            
                if (!isValid)
                {
                    return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
                }
                
                if (ModelState.IsValid)
                {
                    if (teacher.Name != null && teacher.Phone != null && teacher.Email != null
                    && teacher.TheDate != null && (teacher.IdNumber != null || teacher.IdNumber != null) && teacher.City != null && teacher.Area != null)
                    {
                        if (_context.Teachers.Any(t => t.IdNumber == teacher.IdNumber))
                        {
                            return Conflict(new ApiResponse { Success = false, Message = "رقم الهوية موجود مسبقاً" });
                        }

                        if (!Regex.IsMatch(teacher.IdNumber.ToString(), @"^[1-9][0-9]{8}$"))
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "رقم الهوية يجب ان يكون 9 ارقام" });
                        }

                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var birthDate = teacher.TheDate.Value;
                        int age = today.Year - birthDate.Year;
                        if (birthDate > today || !(age > 18 && age < 65) || birthDate.AddYears(age) > today)
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "يجب ان يكون المعلم اكبر من 18 سنة واقل من 65 سنة" });
                        }

                        teacher.Name = NormalizeArabic(teacher.Name);
                        if (_context.Teachers.Any(t => t.Name == teacher.Name))
                        {
                            return Conflict(new ApiResponse { Success = false, Message = "الاسم موجود مسبقا" });
                        }

                        if (!await _emailValidator.IsEmailValidAsync(teacher.Email))
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "البريد الالكتروني غير سليم" });
                        }

                        teacher.IdSchool = school;
                        teacher.IsDeleted = false;
                        _context.Teachers.Add(teacher);

                        await _context.SaveChangesAsync();
                        string studentsKey = $"Teachers_School_{school}";
                        await _cache.RemoveAsync(studentsKey);
                        return Ok();

                    }
                    return BadRequest(new ApiResponse { Success = false, Message = "هناك حقول فارغة" });
                }
                return BadRequest(new ApiResponse { Success = false, Message = "البيانات  غير صالحة" });

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/PostCreateTeacher");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
        }

        [HttpPost("AddTeacherToClassesAndLectuers")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> AddTeacherToClassesAndLectuers([FromBody] List<AddTeacherToClassLectuers> dataTeacherToClassAndLectuers)
        {

            Console.WriteLine("============================123456789======================================");
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TeacherApi/AddTeacherToClassesAndLectuers");
        
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (!dataTeacherToClassAndLectuers.Any())
            {
                await _logger.LogAsync(new Exception("المعرف المرسل فارغ"), "TeacherApi/AddTeacherToClassesAndLectuers");
                return BadRequest(new ApiResponse { Success = false, Message = "المعرف المرسل فارغ." });
            }
            try
            {
                foreach (var dataTeacher in dataTeacherToClassAndLectuers)
                {
                    if (dataTeacher.idTeacher == null || dataTeacher.idClass == null)
                    {
                        await _logger.LogAsync(new Exception("المعرف المرسل فارغ"), "TeacherApi/AddTeacherToClassesAndLectuers");
                        return BadRequest(new ApiResponse { Success = false, Message = "المعرف المرسل فارغ." });
                    }
                    Guid idTeacher;
                    Guid idClass;
                    Console.WriteLine("==================================================================");
                    Console.WriteLine($"Id Teacher: {dataTeacher.idTeacher}, Id Class: {dataTeacher.idClass}");

                    try
                    {
                        idTeacher = dataTeacher.idTeacher;
                        idClass = dataTeacher.idClass;

                    }
                    catch (Exception ex)
                    {
                        await _logger.LogAsync(ex, "TeacherApi/AddTeacherToClassesAndLectuers");
                        return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
                    }

                    if (_context.Teachers.Any(t => t.Id == idTeacher && t.IdSchool == school && t.IsDeleted == false) == false)
                    {
                        await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/AddTeacherToClassesAndLectuers");
                        return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالبيانات المرسلة." });
                    }

                    if (_context.TheClasses.Any(c => c.Id == idClass && c.IdSchool == school && c.IsDeleted == false) == false)
                    {
                        await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/AddTeacherToClassesAndLectuers");
                        return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالبيانات المرسلة." });
                    }

                    if (dataTeacher.lectuers != null && dataTeacher.lectuers.Any())
                    {
                        foreach (var lectuerId in dataTeacher.lectuers)
                        {
                            if (lectuerId == null) continue;

                            Guid idLectuer;
                            Console.WriteLine($"Id lectuer: {lectuerId}");
                            Console.WriteLine("==================================================================");

                            try
                            {
                                idLectuer = lectuerId;

                            }
                            catch (Exception ex)
                            {
                                await _logger.LogAsync(ex, "TeacherApi/AddTeacherToClassesAndLectuers");
                                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
                            }

                            if (_context.Lectuers.Any(l => l.Id == idLectuer && l.IdSchool == school && l.IsDeleted == false) == false)
                            {
                                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/AddTeacherToClassesAndLectuers");
                                return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالبيانات المرسلة." });
                            }

                            var conflictingAssignment = await _context.TeacherLectuerClasses.AsNoTracking()
                                .Where(tlc => tlc.IdSchool == school && tlc.IdClass == idClass && tlc.IdLectuer == idLectuer &&
                                    tlc.IdTeacher != idTeacher && !tlc.IsDeletedTeacherLectuerClass && !tlc.IsDeletedTeacher &&
                                    !tlc.IsDeletedClass && !tlc.IsDeletedLectuer && !tlc.IsDeletedSchool &&
                                    !tlc.IsTeacherRemovedFromClass && !tlc.IsTeacherRemovedFromLectuer)
                                .Select(tlc => tlc.IdTeacherNavigation != null ? tlc.IdTeacherNavigation.Name : null)
                                .FirstOrDefaultAsync();
                            if (conflictingAssignment != null)
                            {
                                return Conflict(new ApiResponse { Success = false, Message = $"لا يمكن إسناد المادة لهذا الصف؛ فهي مسندة بالفعل للمعلم {conflictingAssignment}." });
                            }
                            if (_context.TeacherLectuerClasses.Any(tlc => tlc.IdTeacher == idTeacher && tlc.IdClass == idClass && tlc.IdLectuer == idLectuer && tlc.IdSchool == school) == false)
                            {
                                TeacherLectuerClass teacherLectuerClass = new TeacherLectuerClass
                                {
                                    IdTeacher = idTeacher,
                                    IdClass = idClass,
                                    IdLectuer = idLectuer,
                                    IdSchool = school,
                                    IsDeletedTeacher = false,
                                    IsDeletedClass = false,
                                    IsDeletedLectuer = false,
                                    IsDeletedTeacherLectuerClass = false,
                                    IsTeacherRemovedFromClass = false,
                                    IsTeacherRemovedFromLectuer = false
                                };
                                _context.TeacherLectuerClasses.Add(teacherLectuerClass);
                            }
                            else
                            {
                                var existingEntry = await _context.TeacherLectuerClasses
                                    .Where(tlc => tlc.IdTeacher == idTeacher && tlc.IdClass == idClass && tlc.IdLectuer == idLectuer && tlc.IdSchool == school)
                                    .FirstOrDefaultAsync();

                                if (existingEntry != null && (existingEntry.IsDeletedTeacherLectuerClass == true || existingEntry.IsDeletedTeacher == true || existingEntry.IsDeletedClass == true || existingEntry.IsDeletedLectuer == true || existingEntry.IsTeacherRemovedFromClass == true || existingEntry.IsTeacherRemovedFromLectuer == true))
                                {
                                    existingEntry.IsDeletedTeacherLectuerClass = false;
                                    existingEntry.IsDeletedTeacher = false;
                                    existingEntry.IsDeletedLectuer = false;
                                    existingEntry.IsDeletedClass = false;
                                    existingEntry.IsTeacherRemovedFromClass = false;
                                    existingEntry.IsTeacherRemovedFromLectuer = false;
                                }
                            }
                        }
                    }
                }
                await _context.SaveChangesAsync();
                Console.WriteLine("==================================================================");
                Console.WriteLine("Done");
                Console.WriteLine("==================================================================");
                return Ok();
            }
            catch(Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/AddTeacherToClassesAndLectuers");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
            
        }

        [HttpDelete("RemoveTeacherToClassLectuers")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> RemoveTeacherToClassLectuers([FromBody] RemoveTeacherToClassLectuers dataTeacherToClassAndLectuers)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TeacherApi/RemoveTeacherToClassLectuers");
        
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (dataTeacherToClassAndLectuers.idLectuer == null || dataTeacherToClassAndLectuers.idTeacher == null || dataTeacherToClassAndLectuers.idClass == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/RemoveTeacherToClassLectuers");
                return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالبيانات المرسلة." });
            }
            
            Guid idTeacher;
            Guid idClass;
            Guid idLectuer;
            try
            {
                idTeacher = dataTeacherToClassAndLectuers.idTeacher;
                idClass = dataTeacherToClassAndLectuers.idClass;
                idLectuer = dataTeacherToClassAndLectuers.idLectuer;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/RemoveTeacherToClassLectuers");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
            
            try
            {
                Console.WriteLine($"_context = {_context}");
                Console.WriteLine($"_context.TeacherLectuerClasses = {_context.TeacherLectuerClasses}");
                Console.WriteLine($"school = {school}");
                TeacherLectuerClass? existingTeacherLectruerClass = await _context.TeacherLectuerClasses.Where(tlc =>
                tlc.IdTeacher == idTeacher && tlc.IdClass == idClass && tlc.IdLectuer == idLectuer && tlc.IdSchool == school &&
                tlc.IsDeletedTeacherLectuerClass == false && tlc.IsDeletedTeacher == false && tlc.IsDeletedClass == false && tlc.IsDeletedLectuer == false &&
                tlc.IsTeacherRemovedFromClass == false && tlc.IsTeacherRemovedFromLectuer == false
                ).FirstOrDefaultAsync();

                if (existingTeacherLectruerClass == null)
                {
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/RemoveTeacherToClassLectuers");
                    return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالبيانات المرسلة." });
                }

                existingTeacherLectruerClass.IsDeletedTeacherLectuerClass = true;
                existingTeacherLectruerClass.IsTeacherRemovedFromClass = true;
                existingTeacherLectruerClass.IsTeacherRemovedFromLectuer = true;

                List<Attendance> attendances = await _context.Attendances.Where(a =>
                a.IdTeacher == idTeacher && a.IdClass == idClass && a.IdLectuer == idLectuer && a.IdSchool == school &&
                a.IsDeletedAttendance == false && a.IsDeletedTeacher == false && a.IsDeletedClass == false && a.IsDeletedLectuer == false &&
                a.IsTeacherRemovedFromClass == false && a.IsTeacherRemovedFromLectuer == false
                ).ToListAsync();
                foreach (var item in attendances)
                {
                    item.IsTeacherRemovedFromClass = true;
                    item.IsTeacherRemovedFromLectuer = true;
                }

                List<Grade> grades = await _context.Grades.Where(g =>
                g.IdTeacher == idTeacher && g.IdClass == idClass && g.IdLectuer == idLectuer && g.IdSchool == school &&
                g.IsDeletedGrades == false && g.IsDeletedTeacher == false && g.IsDeletedClass == false && g.IsDeletedLectuer == false &&
                g.IsTeacherRemovedFromClass == false && g.IsTeacherRemovedFromLectuer == false
                ).ToListAsync();
                foreach (var item in grades)
                {
                    item.IsTeacherRemovedFromClass = true;
                    item.IsTeacherRemovedFromLectuer = true;
                }

                List<StudentLectuerTeacher> studentLectuerTeachers = await _context.StudentLectuerTeachers.Where(g =>
                g.IdTeacher == idTeacher && g.IdClass == idClass && g.IdLectuer == idLectuer && g.IdSchool == school &&
                g.IsDeletedStudentLectuerTeacher == false && g.IsDeletedTeacher == false && g.IsDeletedClass == false && g.IsDeletedLectuer == false &&
                g.IsTeacherRemovedFromClass == false && g.IsTeacherRemovedFromLectuer == false
                ).ToListAsync();
                foreach (var item in studentLectuerTeachers)
                {
                    item.IsTeacherRemovedFromClass = true;
                    item.IsTeacherRemovedFromLectuer = true;
                }

                await _context.SaveChangesAsync();
                
                return Ok();
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/RemoveTeacherToClassLectuers");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
            
        }

        [HttpGet("Details")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<ActionResult<DetailsTeacherInSchool>> GetDetailsTeacher(Guid? id)
        {
            try
            {
                // التحقق من صلاحية المستخدم
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TeacherApi/GetDetailsTeacher");
            
                if (!isValid)
                {
                    return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
                }
                
                if (id == null)
                {
                    await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "TeacherApi/GetDetailsTeacher");
                    return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ." });
                }

                Guid Id;

                try
                {
                    Id = id ?? Guid.Empty;

                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(ex, "TeacherApi/GetDetailsTeacher");
                    return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
                }

                var teacher = await _context.Teachers.Include(t => t.IdSchoolNavigation)
                    .Where(m => m.Id == Id && m.IsDeleted == false && m.IdSchool == school)
                    .FirstOrDefaultAsync();
                if (teacher == null)
                {
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/GetDetailsTeacher");
                    return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات لمرسلة." });
                }

                DetailsTeacherInSchool teacherViewModel = new DetailsTeacherInSchool
                {
                    idTeacher = id,
                    nameTeacher = teacher.Name,
                    phone = teacher.Phone,
                    emailAddressTeacher = teacher.Email,
                    theDate = teacher.TheDate,
                    idNumber = teacher.IdNumber,
                    address = teacher.City + "/ " + teacher.Area,
                    nameSchool = teacher.IdSchoolNavigation?.Name ?? "غير معرف",
                };

                return Ok(teacherViewModel);
            }
            catch(Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/GetDetailsTeacher");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("Edit")]
        public async Task<ActionResult<GetEditTeacherInSchool>> GetEditTeacher(Guid? id)
        {
            
            try
            {
                // التحقق من صلاحية المستخدم
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TeacherApi/GetEditTeacher");
            
                if (!isValid)
                {
                    return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
                }

                if (id == null)
                {
                    await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "TeacherApi/GetEditTeacher");
                    return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
                }

                Guid Id;

                try
                {
                    Id = id ?? Guid.Empty;

                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(ex, "TeacherApi/GetEditTeacher");
                    return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع"});
                }

                Teacher? teacher = await _context.Teachers.Include(t => t.IdSchoolNavigation).Where(t => t.Id == Id).FirstOrDefaultAsync();
                if (teacher == null)
                {
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/GetEditTeacher");
                    return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالبيانات لمرسلة"});
                }
                GetEditTeacherInSchool teacherViewModel = new GetEditTeacherInSchool
                {
                    idTeacher = id,
                    nameTeacher = teacher.Name,
                    phone = teacher.Phone,
                    emailAddressTeacher = teacher.Email,
                    theDate = teacher.TheDate,
                    idNumber = teacher.IdNumber,
                    city = teacher.City,
                    area = teacher.Area,
                    nameSchool = teacher.IdSchoolNavigation?.Name ?? "غير معرفة",
                };

                return Ok(teacherViewModel);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/GetEditTeacher");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpPut("Edit")]
        public async Task<IActionResult> PostEditTeacher([FromBody] PostEditTeacherInSchool teacher)
        {
            // التحقق من صلاحية المستخدم
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TeacherApi/PostEditTeacher");
        
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (teacher.idTeacher == null)
            {
                await _logger.LogAsync(new Exception("المعرف المرسل فارغ"), "TeacherApi/PostEditTeacher");
                return BadRequest(new ApiResponse { Success = false, Message = "المعرف المرسل فارغ." });
            }

            Guid Id;

            try
            {
                Id = teacher.idTeacher ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/PostEditTeacher");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }

            try
            {
                if (ModelState.IsValid)
                {
                    if (teacher.nameTeacher != null && teacher.phone != null && teacher.emailAddressTeacher != null
                    && teacher.theDate != null && (teacher.idNumber != null || teacher.idNumber != null) && teacher.city != null && teacher.area != null)
                    {
                        if (_context.Teachers.Any(s => s.IdNumber == teacher.idNumber && s.Id != Id))
                        {
                            return Conflict(new ApiResponse { Success = false, Message = "رقم الهوية موجود مسبقاً" });
                        }

                        if (!Regex.IsMatch(teacher.idNumber.ToString(), @"^[0-9][0-9]{8}$"))
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "يرجى التأكد من رقم الهوية" });
                        }

                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var birthDate = teacher.theDate.Value;
                        int age = today.Year - birthDate.Year;
                        if (birthDate > today.AddYears(-age)) age--;

                        if (birthDate > today || age < 18 || age > 65)
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "يجب ان يكون المعلم اكبر من 18 واصغر من 65" });
                        }

                        teacher.nameTeacher = NormalizeArabic(teacher.nameTeacher);
                        if (_context.Teachers.Any(s => s.Name == teacher.nameTeacher && s.Id != Id))
                        {
                            return Conflict(new ApiResponse { Success = false, Message = "الاسم موجود مسبقا" });
                        }

                        if (!await _emailValidator.IsEmailValidAsync(teacher.emailAddressTeacher))
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "البريد الالكتروني غير سليم" });
                        }

                        Teacher? teacher1 = await _context.Teachers.Where(teach => teach.Id == Id && teach.IdSchool == school && teach.IsDeleted == false).FirstOrDefaultAsync();
                        if (teacher1 == null)
                        {
                            await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/PostEditTeacher");
                            return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالمعرف" }); ;
                        }

                        teacher1.Name = teacher.nameTeacher;
                        teacher1.Phone = teacher.phone;
                        teacher1.Email = teacher.emailAddressTeacher;
                        teacher1.TheDate = teacher.theDate;
                        teacher1.City = teacher.city;
                        teacher1.Area = teacher.area;
                        teacher1.IdNumber = teacher.idNumber;
                        teacher1.IdSchool = school;
                        await _context.SaveChangesAsync();
                        string teachersKey = $"Teachers_School_{school}";
                        await _cache.RemoveAsync(teachersKey);
                        return Ok();
                    }
                
                }

                return BadRequest(new ApiResponse { Success = false, Message = "البيانات غير مكتملة أو غير صالحة" }); ;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/PostEditTeacher");
                return StatusCode(500,new { success = false, message = "حدث خطأ أثناء تحديث البيانات." });
            }
        }


        [HttpGet("ManagerStudentToTeacher")]
        [AuthorizeRoles("Teacher")]
        public async Task<IActionResult> ManagerStudentToTeacher(
            Guid? teacherId,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            if (!teacherId.HasValue || teacherId.Value == Guid.Empty)
                return BadRequest(new { error = "A valid teacher id is required." });

            Guid Id;

            try
            {
                Id = teacherId ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/DownloadTeacherCertificate");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }

            try
            {
                Console.WriteLine($"Id Teach: {teacherId}");
                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdTeacher, IdSchool,status) = await _sessionValidatorService.ValidateTeacherSessionAsync(HttpContext, Id, "Attendance/DataAttendance");
                if (!IsValid)
                {
                    return Forbid();
                }

                //فحص اذا كان تم ارسال قيمة المتغير ام لا و وصع قيمة افتراضية اذا كان لا
                if (length <= 0)
                    length = 10;
                length = Math.Min(length, 100);
                start = Math.Max(start, 0);

                // تحديد قيمة الـ searchValue
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var baseQuery = _context.StudentLectuerTeachers.Where(std =>
                    std.IdSchool == IdSchool && std.IdTeacher == IdTeacher &&
                    !std.IsDeletedStudentLectuerTeacher && !std.IsDeletedStudent &&
                    !std.IsDeletedClass && !std.IsDeletedLectuer && !std.IsDeletedTeacher &&
                    !std.IsDeletedSchool &&
                    !std.IsTeacherRemovedFromClass && !std.IsTeacherRemovedFromLectuer);

                var totalRecords = await baseQuery.CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = baseQuery
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "Unknown",
                        IdStudent = s.IdStudent,
                        StudentName = s.IdStudentNavigation != null ? s.IdStudentNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        IdClass = s.IdClass ?? Guid.Empty
                        
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.StudentName != null && s.StudentName.Contains(searchValue))||
                        (s.LectuerName != null && s.LectuerName.Contains(searchValue))||
                        (s.ClassroomName != null && s.ClassroomName.Contains(searchValue))
                    );
                }

                // عدد السجلات الاصلية التي تنطبق عليها الشروط
                var filteredCount = string.IsNullOrWhiteSpace(searchValue)
                    ? totalRecords
                    : await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.StudentName),
                    ("0", "desc") => query.OrderByDescending(s => s.StudentName),
                    ("1", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("1", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    ("2", "asc") => query.OrderBy(s => s.LectuerName),
                    ("2", "desc") => query.OrderByDescending(s => s.LectuerName),
                    _ => query.OrderBy(s => s.StudentName)
                };

                // التقطيع (Pagination)
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                //ارسال بيانات للعرض
                var students = data.
                Select(s => new ManagerMenegarStudentInClassViewModel
                {
                    Id = s.Id,
                    ClassroomName = s.ClassroomName,
                    StudentName = s.StudentName,
                    IdClass = s.IdClass,
                    LectuerName = s.LectuerName
                    })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = students
                };
                
                return Json(result);
            }
            catch (Exception e)
            {
                // حال كان هناك خطأ غير متوقع
                await _logger.LogAsync(e, "Attendance/DataAttendance");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Unable to load students." });
            }
        }


        [HttpGet("grade-distribution")]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> GetGradeDistribution(Guid? idTeacher)
        {
            if (!idTeacher.HasValue || idTeacher.Value == Guid.Empty)
                return BadRequest(new { error = "A valid teacher id is required." });

            try
            {
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, idTeacher.Value,
                        "TeacherApi/GetGradeDistribution");
                if (!isValid)
                    return Forbid();

                var gradeStats = await _context.Grades.AsNoTracking()
                    .Where(grade => grade.IdSchool == schoolId && grade.IdTeacher == teacherId &&
                        grade.IdLectuer.HasValue && !grade.IsDeletedGrades &&
                        !grade.IsDeletedStudent && !grade.IsDeletedTeacher &&
                        !grade.IsDeletedLectuer && !grade.IsDeletedSchool &&
                        !grade.IsTeacherRemovedFromLectuer)
                    .GroupBy(grade => grade.IdLectuer!.Value)
                    .Select(group => new
                    {
                        LectuerId = group.Key,
                        TotalStudents = group.Count(),
                        Below50Count = group.Count(grade => grade.Total < 50),
                        Below60Count = group.Count(grade => grade.Total >= 50 && grade.Total < 60),
                        Below70Count = group.Count(grade => grade.Total >= 60 && grade.Total < 70),
                        Below80Count = group.Count(grade => grade.Total >= 70 && grade.Total < 80),
                        Below90Count = group.Count(grade => grade.Total >= 80 && grade.Total < 90),
                        Below100Count = group.Count(grade => grade.Total >= 90 && grade.Total < 100),
                        Equal100Count = group.Count(grade => grade.Total == 100)
                    })
                    .ToListAsync();

                var lectureIds = gradeStats.Select(item => item.LectuerId).ToList();
                var lectureNames = await _context.Lectuers.AsNoTracking()
                    .Where(lecture => lecture.IdSchool == schoolId && lectureIds.Contains(lecture.Id) &&
                        !lecture.IsDeleted && !lecture.IsDeletedSchool)
                    .Select(lecture => new { lecture.Id, lecture.Name })
                    .ToDictionaryAsync(lecture => lecture.Id, lecture => lecture.Name);

                return Ok(gradeStats.Select(item => new
                {
                    LectuerName = lectureNames.GetValueOrDefault(item.LectuerId, "Unknown"),
                    item.TotalStudents,
                    Below50 = item.Below50Count * 100.0 / item.TotalStudents,
                    Below60 = item.Below60Count * 100.0 / item.TotalStudents,
                    Below70 = item.Below70Count * 100.0 / item.TotalStudents,
                    Below80 = item.Below80Count * 100.0 / item.TotalStudents,
                    Below90 = item.Below90Count * 100.0 / item.TotalStudents,
                    Below100 = item.Below100Count * 100.0 / item.TotalStudents,
                    Equal100 = item.Equal100Count * 100.0 / item.TotalStudents
                }));
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/GetGradeDistribution");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Unable to load grade statistics." });
            }
        }

        [HttpGet("attendance-summary")]
        [AuthorizeRoles(RoleNames.Teacher)]
        public async Task<IActionResult> GetAttendanceSummary(Guid? idTeacher)
        {
            if (!idTeacher.HasValue || idTeacher.Value == Guid.Empty)
                return BadRequest(new { error = "A valid teacher id is required." });

            try
            {
                var (isValid, teacherId, schoolId, _) = await _sessionValidatorService
                    .ValidateTeacherSessionAsync(HttpContext, idTeacher.Value,
                        "TeacherApi/GetAttendanceSummary");
                if (!isValid)
                    return Forbid();

                var attendanceStats = await _context.Attendances.AsNoTracking()
                    .Where(attendance => attendance.IdSchool == schoolId &&
                        attendance.IdTeacher == teacherId && attendance.IdLectuer.HasValue &&
                        !attendance.IsDeletedAttendance && !attendance.IsDeletedStudent &&
                        !attendance.IsDeletedLectuer && !attendance.IsDeletedTeacher &&
                        !attendance.IsDeletedSchool && !attendance.IsTeacherRemovedFromLectuer)
                    .GroupBy(attendance => attendance.IdLectuer!.Value)
                    .Select(group => new
                    {
                        LectuerId = group.Key,
                        TotalSessions = group.Count(),
                        AttendanceCount = group.Count(item => item.AttendanceStatus == "1"),
                        AbsenceCount = group.Count(item => item.AttendanceStatus == "0"),
                        ExcusedAbsenceCount = group.Count(item => item.AttendanceStatus == "m")
                    })
                    .ToListAsync();

                var lectureIds = attendanceStats.Select(item => item.LectuerId).ToList();
                var lectureNames = await _context.Lectuers.AsNoTracking()
                    .Where(lecture => lecture.IdSchool == schoolId && lectureIds.Contains(lecture.Id) &&
                        !lecture.IsDeleted && !lecture.IsDeletedSchool)
                    .Select(lecture => new { lecture.Id, lecture.Name })
                    .ToDictionaryAsync(lecture => lecture.Id, lecture => lecture.Name);

                return Ok(attendanceStats.Select(item => new
                {
                    LectuerName = lectureNames.GetValueOrDefault(item.LectuerId, "Unknown"),
                    item.TotalSessions,
                    AttendancePercentage = item.AttendanceCount * 100.0 / item.TotalSessions,
                    AbsencePercentage = item.AbsenceCount * 100.0 / item.TotalSessions,
                    ExcusedAbsencePercentage = item.ExcusedAbsenceCount * 100.0 / item.TotalSessions
                }));
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/GetAttendanceSummary");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { error = "Unable to load attendance statistics." });
            }
        }

        [NonAction]
        public async Task<IActionResult> GetStudentCountPerGrades(Guid? idTeacher)
        {
            Guid Id;

            try
            {
                Id = idTeacher ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }

            Console.WriteLine($"Id Teacher: {idTeacher}");
            var schoolId = HttpContext.Session.GetGuid("School");
            Console.WriteLine($"Id School: {schoolId}");
            var data = _context.Grades
                .Where(g =>
                    g.IdSchool == schoolId &&
                    g.IdTeacher == Id
                    && g.IdStudentNavigation != null 
                    &&(g.IdStudentNavigation.IsDeletedStudent == false || g.IdStudentNavigation.IsDeletedStudent == null) &&
                    g.IdLectuerNavigation != null
                    ).Include(l => l.IdClassNavigation)
                .GroupBy(g => new { g.IdLectuer, g.IdLectuerNavigation.Name })
                .Select(g => new
                {
                    LectuerName = g.Key.Name,
                    TotalStudents = g.Count(),
                    Below50 = g.Count(x => x.Total < 50) * 100.0 / g.Count(),
                    Below60 = g.Count(x => x.Total >= 50 && x.Total < 60) * 100.0 / g.Count(),
                    Below70 = g.Count(x => x.Total >= 60 && x.Total < 70) * 100.0 / g.Count(),
                    Below80 = g.Count(x => x.Total >= 70 && x.Total < 80) * 100.0 / g.Count(),
                    Below90 = g.Count(x => x.Total >= 80 && x.Total < 90) * 100.0 / g.Count(),
                    Below100 = g.Count(x => x.Total >= 90 && x.Total < 100) * 100.0 / g.Count(),
                    Equal100 = g.Count(x => x.Total == 100) * 100.0 / g.Count()
                })
                .ToList();
            if (!data.Any())
            {
                return Json(new { error = "No data available" });
            }


            return Json(data);
        }

        [NonAction]
        public async Task<IActionResult> GetStudentCountPerAttendance(Guid? idTeacher)
        {
            Guid Id;

            try
            {
                Id = idTeacher ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/Details");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }

            var schoolId = HttpContext.Session.GetGuid("School");

            var data = _context.Attendances
                .Where(g => g.IdSchool == schoolId
                            && g.IdTeacher == Id
                            && g.IdStudentNavigation != null
                            && (g.IdStudentNavigation.IsDeletedStudent == false || g.IdStudentNavigation.IsDeletedStudent == null)
                            && g.IdLectuerNavigation != null)
                .GroupBy(g => new { g.IdLectuer, g.IdLectuerNavigation.Name })
                .Select(g => new
                {
                    LectuerName = g.Key.Name,
                    TotalSessions = g.Count(),
                    AttendancePercentage = g.Count(x => x.AttendanceStatus == "1") * 100.0 / g.Count(),
                    AbsencePercentage = g.Count(x => x.AttendanceStatus == "0") * 100.0 / g.Count(),
                    ExcusedAbsencePercentage = g.Count(x => x.AttendanceStatus == "m") * 100.0 / g.Count()
                })
                .ToList();

            return Json(data);
        }

        // شهادة قيد لمعلم
        [NonAction]
        public async Task<IActionResult> DownloadTeacherCertificate(Guid? idTeacher)
        {
            Guid Id;

            try
            {
                Id = idTeacher ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Teacher/DownloadTeacherCertificate");
                _notyf.Error("حدث خطأ غير متوقع.");
                return RedirectToAction("ManagerMenegarTeacherView","Menegar");
            }
            try
            {
                Teacher? teacher = _context.Teachers
                .Where(s => s.Id == Id && s.IsDeleted == false && s.IdSchool == HttpContext.Session.GetGuid("School"))
                .Include(s => s.IdSchoolNavigation).SingleOrDefault();
                if (teacher == null)
                {
                    await _logger.LogAsync(new Exception("انتهت صلاحية الجلسة"), "Teacher/DownloadTeacherCertificate");
                    _notyf.Error("انتهت الجلسة.");
                    return RedirectToAction("Logout", "Account");
                }
                Menegar? menegar = _context.Menegars.SingleOrDefault(m => m.IdSchool == teacher.IdSchool);

                var document = new TeacherEnrollmentCertificate(
                    teacher?.Name ?? "غير معرف",
                    teacher?.IdNumber ?? 0,
                    teacher?.IdSchoolNavigation?.Name ?? "غير معرف",
                    menegar?.Name ?? "لم يتم اعتماده بعد.",
                    _context.TeacherLectuerClasses.Where(tl => tl.IdTeacher == Id && teacher.IdSchool == teacher.IdSchool).Select(name => name.IdLectuerNavigation.Name).ToList());
                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                return File(stream, "application/pdf", $"شهادة_قيد_{teacher?.Name ?? "غير معرف"}.pdf");

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Student/DownloadStudentCertificate");
                _notyf.Error("حدث خطا اثناء انشاء شهادة قيد.\nيرجى المحاولة لاحقا");
                return View(nameof(Index));
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

        // POST: Teacher/Delete/5
        [HttpDelete("Delete")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> DeleteTeacher([FromBody] DeleteInSchool deleteTeacherInSchool)
        {
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TeacherApi/DeleteTeacher");
            
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (deleteTeacherInSchool.id == null)
            {
                await _logger.LogAsync(new Exception("المعرف المرسل فارغ"), "TeacherApi/DeleteTeacher");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = deleteTeacherInSchool.id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/DeleteTeacher");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
            
            Teacher? teacher = await _context.Teachers.Where(teach => teach.Id == Id && teach.IdSchool == school && teach.IsDeleted == false).FirstOrDefaultAsync();
            if (teacher == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/DeleteTeacher");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات المرسلة" });
            }

            List<TeacherLectuerClass> teacherLectuerClass = await _context.TeacherLectuerClasses.Where(tlc => tlc.IdTeacher == Id && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
            foreach (var teachers in teacherLectuerClass)
            {
                teachers.IsDeletedTeacher = true;
            }

            List<Grade> grades = await _context.Grades.Where(tlc => tlc.IdTeacher == Id && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
            foreach (var grade in grades)
            {
                grade.IsDeletedTeacher = true;
            }

            List<Attendance> attendances = await _context.Attendances.Where(tlc => tlc.IdTeacher == Id && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
            foreach (var attendance in attendances)
            {
                attendance.IsDeletedTeacher = true;
            }

            teacher.IsDeleted = true;

            ApplicationUser? account = teacher.ApplicationUserId.HasValue
                ? await _context.Users.FirstOrDefaultAsync(a => a.Id == teacher.ApplicationUserId.Value && a.IsActive)
                : null;
            if (account != null)
            {
                account.IsActive = false;
            }

            string teachersKey = $"Teachers_School_{school}";
            await _cache.RemoveAsync(teachersKey);
            await _context.SaveChangesAsync();
            return Ok();

        }

        /*[HttpDelete("DeleteTeacher")]
        public async Task<IActionResult> DeleteTeacherInClass([FromBody] DeleteInSchool deleteTeacherInClass)
        {
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "TeacherApi/DeleteTeacherInClass");
            
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (deleteTeacherInClass.id == null)
            {
                await _logger.LogAsync(new Exception("المعرف المرسل فارغ"), "TeacherApi/DeleteTeacherInClass");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = deleteTeacherInClass.id;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "TeacherApi/DeleteTeacherInClass");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }

            Teacher? teacher = await _context.Teachers.Where(teach => teach.Id == Id && teach.IdSchool == school && teach.IsDeleted == false).FirstOrDefaultAsync();
            if (teacher == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "TeacherApi/DeleteTeacherInClass");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات المرسلة" });
            }
            
            List<TeacherLectuerClass> teacherLectuerClass = await _context.TeacherLectuerClasses.Where(tlc => tlc.IdTeacher == Id && tlc.IsTeacherRemovedFromClass == false && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
            foreach (var teachers in teacherLectuerClass)
            {
                teachers.IsTeacherRemovedFromClass = true;
            }

            List<StudentLectuerTeacher> studentLectuerTeachers = await _context.StudentLectuerTeachers.Where(tlc => tlc.IdTeacher == Id && tlc.IsTeacherRemovedFromClass == false && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
            foreach (var teachers in studentLectuerTeachers)
            {
                teachers.IsTeacherRemovedFromClass = true;
            }

            List<Grade> grade = await _context.Grades.Where(tlc => tlc.IdTeacher == Id && tlc.IsTeacherRemovedFromClass == false && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
            foreach (var teachers in grade)
            {
                teachers.IsTeacherRemovedFromClass = true;
            }

            List<Attendance> attendances = await _context.Attendances.Where(tlc => tlc.IdTeacher == Id && tlc.IsTeacherRemovedFromClass == false && tlc.IdSchool == school && tlc.IsDeletedTeacher == false).ToListAsync();
            foreach (var teachers in attendances)
            {
                teachers.IsTeacherRemovedFromClass = true;
            }
            
            await _context.SaveChangesAsync();
            return Ok();

        }
*/
        private bool TeacherExists(Guid id)
        {
            return _context.Teachers.Any(e => e.Id == id);
        }
    }
}

