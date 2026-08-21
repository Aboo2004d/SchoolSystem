using System;
using System.Collections.Generic;
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
using Microsoft.Extensions.Caching.Distributed;
using NuGet.Protocol;
using QuestPDF.Fluent;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;
using SchoolSystem.Models.AdminSchool;

namespace SchoolSystem.Controllers
{
    [ApiController]
    [Route("api/student")]
    public class StudentApiController : Controller
    {
        private readonly SystemSchoolDbContext _context;

        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;
        private readonly IDistributedCache _cache;
        private readonly IEmailValidationService _emailValidator;
        private readonly IAutomaticAccountService _accounts;


        public StudentApiController(SystemSchoolDbContext context, IDistributedCache cache, IEmailValidationService emailValidator, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger, IAutomaticAccountService accounts)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
            _emailValidator = emailValidator;
            _cache = cache;
            _accounts = accounts;

        }

        [HttpPost("Create")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        
        public async Task<IActionResult> PostCreateStudent([FromBody] Student student)
        {
            try
            {
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "StudentApi/PostCreateStudent");
            
                if (!isValid)
                {
                    return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
                }
                
                if (ModelState.IsValid)
                {
                    if (student.Name != null && student.Phone != null
                    && student.TheDate != null && student.IdClass != null && student.IdNumber != null && student.City != null && student.Area != null)
                    {
                        if (_context.Students.Any(s => s.IdNumber == student.IdNumber))
                        {
                            return Conflict(new ApiResponse { Success = false, Message = "رقم الهوية موجود مسبقاً" });
                        }

                        if (!Regex.IsMatch(student.IdNumber.ToString(), @"^[1-9][0-9]{8}$"))
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "رقم الهوية يجب ان يكون 9 ارقام" });
                        }

                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var birthDate = student.TheDate.Value;

                        // حساب العمر
                        int age = today.Year - birthDate.Year;
                        if (birthDate > today.AddYears(-age))
                        {
                            age--; // لسه ما مر عيد ميلاده هالسنة
                        }

                        // التحقق
                        if (birthDate > today || age < 5)
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "تاريخ الميلاد غير صالح" });
                        }

                        student.Name = NormalizeArabic(student.Name);
                        if (_context.Students.Any(s => s.Name == student.Name))
                        {
                            return Conflict(new ApiResponse { Success = false, Message = "الاسم موجود مسبقا" });
                        }

                        if (!string.IsNullOrWhiteSpace(student.Email) && !await _emailValidator.IsEmailValidAsync(student.Email))
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "البريد الالكتروني غير سليم" });
                        }

                        var account = await _accounts.CreateAsync(student.IdNumber!.Value, student.Email, RoleNames.Student);
                        if (!account.Success) return Conflict(new ApiResponse { Success = false, Message = account.Message });
                        student.ApplicationUserId = account.User!.Id;
                        student.Email = account.Email;
                        student.IdSchool = school;
                        student.IsDeletedClass = false;
                        student.IsDeletedStudent = false;
                        _context.Students.Add(student);
                        await _context.SaveChangesAsync();
                        Student? std = await _context.Students.Where(sclt => sclt.IdNumber == student.IdNumber).FirstOrDefaultAsync();
                        if (std == null)
                        {
                            await _logger.LogAsync(new Exception("حدث خطأ غير متوقع اثناء حفظ البيانات"), "StudentApi/PostCreateStudent");
                            return StatusCode(500, new ApiResponse { Success = false, Message = "فشلت عملية الحفظ" });
                        }
                        var studentClass = await _context.TeacherLectuerClasses.Where(sclt => sclt.IdClass == std.IdClass && sclt.IdSchool == school && sclt.IsDeletedTeacher== false)
                            .ToListAsync();

                        if (studentClass.Any())
                        {
                            foreach (var item in studentClass)
                            {
                                var studentLectuer = new StudentLectuerTeacher
                                {
                                    IdStudent = std.Id,
                                    IdClass = std.IdClass,
                                    IdTeacher = item.IdTeacher,
                                    IdLectuer = item.IdLectuer,
                                    IdSchool = std.IdSchool,
                                    IsDeletedClass = false,
                                    IsDeletedLectuer = false,
                                    IsDeletedStudent = false,
                                    IsDeletedTeacher = false,
                                    IsDeletedStudentLectuerTeacher = false

                                };
                                _context.StudentLectuerTeachers.Add(studentLectuer);

                            }
                        }
                        await _context.SaveChangesAsync();
                        string studentsKey = $"Students_School_{school}";
                        await _cache.RemoveAsync(studentsKey);
                        return Ok();

                    }
                }
                return BadRequest(new ApiResponse { Success = false, Message = "البيانات غير مكتملة أو غير صالحة" });

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/PostCreateStudent");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
        }

        // GET: Student/Details/5
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("Details")]
        public async Task<ActionResult<DetailsStudentInSchool>> GetDetailsStudent(Guid? id)
        {
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "StudentApi/GetDetailsStudent");
            
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }
                

            if (id == null)
            {
                await _logger.LogAsync(new Exception("معرف فارغ"), "Student/GetDetailsStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;
            try
            {
                Id = id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Student/GetDetailsStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }

            try
            {
                var student = await _context.Students
                    .Include(c => c.IdClassNavigation)
                    .Include(sch => sch.IdSchoolNavigation)
                    .FirstOrDefaultAsync(m => m.Id == Id);

                if (student == null)
                {
                    await _logger.LogAsync(new Exception("التلاعب بمعرف الطالب المرسل"), "Student/GetDetailsStudent");
                    return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بمعرف الطالب المرسل" });
                }

                DetailsStudentInSchool students = new DetailsStudentInSchool
                {
                    idStudent = id,
                    nameStudent = student.Name,
                    phoneStudent = student.Phone,
                    emailAddressStudent = student.Email,
                    nameClass = student.IdClassNavigation?.Name ?? "غير معرف",
                    nameSchool = student.IdSchoolNavigation?.Name ?? "غير معرف",
                    theDate = student.TheDate,
                    idNumber = student.IdNumber,
                    address = student.City + "/ " + student.Area,
                    isDeleted = student.IsDeletedStudent == true ? "غير فعال" : "فعال"
                };

                return Ok(students);

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "Student/GetDetailsStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("Edit")]
        public async Task<ActionResult<GetEditStudentInSchool>> GetEditStudent(Guid? id)
        {
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "StudentApi/GetEditStudent");
            
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }
            
            if (id == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بمعرف الطالب المرسل"), "StudentApi/GetEditStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بمعرف الطالب المرسل" });
            }
            Guid Id;

            try
            {
                Id = id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/GetEditStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
            
            try
            {
                var student = await _context.Students.Include(s => s.IdSchoolNavigation)
                .Where(std => std.Id == Id && std.IdSchool == school && std.IsDeletedStudent == false).FirstOrDefaultAsync();

                if (student == null)
                {
                    await _logger.LogAsync(new Exception("التلاعب ببيانات الطالب المرسلة"), "StudentApi/GetEditStudent");
                    return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالمعرف الخاص بالطالب" });
                }
                
                GetEditStudentInSchool students = new GetEditStudentInSchool
                {
                    idStudent = student.Id,
                    nameStudent = student.Name,
                    nameSchool = student.IdSchoolNavigation?.Name??"غير معرف",
                    phone = student.Phone,
                    emailAddressStudent = student.Email,
                    theDate = student.TheDate,
                    city = student.City,
                    area = student.Area,
                    idNumber = student.IdNumber,
                    idClass = student.IdClass,
                    isDeleted= student.IsDeletedStudent == true ? "غير فعال" : "فعال"
                };

                
                return Ok(students);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/GetEditStudent");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع"});
            }

            
        }

        [HttpPut("Edit")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostEditStudent([FromBody]PostEditStudentInSchool student)
        {
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "StudentApi/PostEditStudent");
            
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (student.idStudent == null)
            {
                await _logger.LogAsync(new Exception("التلاعب بالبيانات الطالب المرسلة"), "StudentApi/PostEditStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب ببيانات الطالب المرسلة" });
            }

            Guid Id;

            try
            {
                Id = student.idStudent ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/PostEditStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع"});
            }

            try
            {
                if (ModelState.IsValid)
                {
                    if (student.nameStudent != null && student.phone != null && student.emailAddressStudent != null
                    && student.theDate != null && student.idClass != null && (student.idNumber != null || student.idNumber != null) && student.city != null && student.area != null)
                    {
                        if (_context.Students.Any(s => s.IdNumber == student.idNumber && s.Id != Id))
                        {
                            return Conflict(new ApiResponse { Success = false, Message = "رقم الهوية موجود مسبقاً" });
                        }

                        if (!Regex.IsMatch(student.idNumber.ToString(), @"^[1-9][0-9]{8}$"))
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "رقم الهوية يجب ان يكون 9 ارقام" });
                        }

                        var today = DateOnly.FromDateTime(DateTime.Today);
                        var birthDate = student.theDate.Value;
                        int age = today.Year - birthDate.Year;
                        if (birthDate > today.AddYears(-age)) age--;

                        if (birthDate > today || age < 5)
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "تاريخ الميلاد غير صالح" });
                        }

                        student.nameStudent = NormalizeArabic(student.nameStudent);
                        if (_context.Students.Any(s => s.Name == student.nameStudent && s.Id != Id))
                        {
                            return Conflict(new ApiResponse { Success = false, Message = "الاسم موجود مسبقا" });
                        }

                        if (!await _emailValidator.IsEmailValidAsync(student.emailAddressStudent))
                        {
                            return BadRequest(new ApiResponse { Success = false, Message = "البريد الالكتروني غير سليم" });
                        }

                        Student? std = await _context.Students.FindAsync(Id);
                        if (std == null)
                        {
                            await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "StudentApi/PostEditStudent");
                            return BadRequest(new ApiResponse { Success = false, Message = "تم التلاعب بالمعرف" }); ;
                        }

                        std.Name = student.nameStudent;
                        std.Phone = student.phone;
                        std.Email = student.emailAddressStudent;
                        std.TheDate = student.theDate;
                        std.City = student.city;
                        std.Area = student.area;
                        std.IdClass = student.idClass;
                        std.IdNumber = student.idNumber;
                        std.IdSchool = school;
                        std.IsDeletedStudent = false;
                        std.IsDeletedClass = false;
                        await _context.SaveChangesAsync();
                        string studentsKey = $"Students_School_{school}";
                        await _cache.RemoveAsync(studentsKey);
                        return Ok();
                    }
                }
                return BadRequest(new ApiResponse { Success = false, Message = "البيانات غير مكتملة أو غير صالحة"});;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/PostEditStudent");
                return StatusCode(500,new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
        }

        [HttpGet("grade-chart")]
        [AuthorizeRoles(RoleNames.Student)]
        public async Task<IActionResult> GetStudentGradeChart(Guid idStudent)
        {
            try
            {
                var (isValid, studentId, schoolId, _) = await _sessionValidatorService
                    .ValidateStudentDataAccessAsync(HttpContext, idStudent, "StudentApi/GradeChart");
                if (!isValid) return Forbid();

                var data = await _context.Grades.AsNoTracking()
                    .Where(g => g.IdSchool == schoolId && g.IdStudent == studentId &&
                        !g.IsDeletedGrades && !g.IsDeletedStudent && !g.IsDeletedLectuer &&
                        !g.IsDeletedSchool && g.IdLectuerNavigation != null)
                    .GroupBy(g => new { g.IdLectuer, g.IdLectuerNavigation!.Name })
                    .Select(group => new
                    {
                        lectuerName = group.Key.Name,
                        totalGrade = group.Average(g => (double?)(g.Total ?? 0)) ?? 0
                    })
                    .OrderBy(item => item.lectuerName)
                    .ToListAsync();

                return Ok(data);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/GradeChart");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "تعذر تحميل الرسم البياني للعلامات." });
            }
        }

        [HttpGet("attendance-chart")]
        [AuthorizeRoles(RoleNames.Student)]
        public async Task<IActionResult> GetStudentAttendanceChart(Guid idStudent)
        {
            try
            {
                var (isValid, studentId, schoolId, _) = await _sessionValidatorService
                    .ValidateStudentDataAccessAsync(HttpContext, idStudent, "StudentApi/AttendanceChart");
                if (!isValid) return Forbid();

                var result = await _context.Attendances.AsNoTracking()
                    .Where(a => a.IdSchool == schoolId && a.IdStudent == studentId &&
                        !a.IsDeletedAttendance && !a.IsDeletedStudent && !a.IsDeletedLectuer &&
                        !a.IsDeletedSchool && a.IdLectuerNavigation != null)
                    .GroupBy(a => new { a.IdLectuer, a.IdLectuerNavigation!.Name })
                    .Select(group => new
                    {
                        subjectName = group.Key.Name,
                        totalSessions = group.Count(),
                        presentCount = group.Count(a => a.AttendanceStatus == "1"),
                        excusedCount = group.Count(a => a.AttendanceStatus == "m")
                    })
                    .OrderBy(item => item.subjectName)
                    .ToListAsync();

                var data = result.Select(item => new
                {
                    item.subjectName,
                    item.totalSessions,
                    item.presentCount,
                    item.excusedCount,
                    presentPercentage = item.totalSessions == 0 ? 0 : Math.Round(item.presentCount * 100.0 / item.totalSessions, 2),
                    excusedPercentage = item.totalSessions == 0 ? 0 : Math.Round(item.excusedCount * 100.0 / item.totalSessions, 2)
                });

                return Ok(data);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/AttendanceChart");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { message = "تعذر تحميل الرسم البياني للحضور." });
            }
        }

        [HttpDelete("Delete")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> DeleteStudent([FromBody] DeleteInSchool deleteStudentInSchool)
        {
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "StudentApi/DeleteStudent");
            
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (deleteStudentInSchool.id == null)
            {
                await _logger.LogAsync(new Exception("المعرف المرسل فارغ"), "StudentApi/DeleteStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }
            
            Guid Id;

            try
            {
                Id = deleteStudentInSchool.id ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/DeleteStudent");
                return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن ارسال معرف فارغ" });
            }

            try
            {
                
                Student? student = await _context.Students.Where(std => std.Id == Id).SingleOrDefaultAsync();
                if (student == null)
                {
                    await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة"), "StudentApi/DeleteStudent");
                    return BadRequest(new ApiResponse { Success = false, Message = "لا يمكن التلاعب بالبيانات المرسلة" });
                }

                student.IsDeletedStudent = true;

                ApplicationUser? account = student.ApplicationUserId.HasValue
                    ? await _context.Users.SingleOrDefaultAsync(s => s.Id == student.ApplicationUserId.Value && s.IsActive)
                    : null;
                if (account != null)
                    account.IsActive = false;
                    
                List<Grade>? grades = await _context.Grades
                    .Where(g => g.IdStudent == Id && g.IdSchool == school && g.IsDeletedStudent == false)
                    .ToListAsync();
                foreach (var grade in grades)
                {
                    grade.IsDeletedStudent = true;
                }

                List<Attendance>? attendances = await _context.Attendances
                    .Where(a => a.IdStudent == Id && a.IdSchool == school && a.IsDeletedStudent == false)
                    .ToListAsync();
                foreach(var attendance in attendances)
                {
                    attendance.IsDeletedStudent = true;
                }

                List<StudentLectuerTeacher>? studentLectuerTeachers = await _context.StudentLectuerTeachers
                    .Where(slt => slt.IdStudent == Id && slt.IdSchool == school && slt.IsDeletedStudent == false)
                    .ToListAsync();
                foreach (var studentLectuerTeacher in studentLectuerTeachers)
                {
                    studentLectuerTeacher.IsDeletedStudent = true;
                }

                string studentsKey = $"Students_School_{school}";
                await _cache.RemoveAsync(studentsKey);
                await _context.SaveChangesAsync();
                return Ok();

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/DeleteStudent");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع." });
            }
        }

        // شهادة قيد لطالب
        [NonAction]
        public IActionResult DownloadStudentCertificate(Guid? idStudent)
        {
            try
            {
                Student? student = _context.Students
                .Where(s => s.Id == idStudent && s.IsDeletedSchool == false && s.IdSchool == HttpContext.Session.GetGuid("School"))
                .Include(s => s.IdClassNavigation).Include(s => s.IdSchoolNavigation).SingleOrDefault();
                if (student == null)
                {
                    _logger.LogAsync(new Exception("انتهت صلاحية الجلسة"), "Student/DownloadStudentCertificate");
                    _notyf.Error("انتهت الجلسة.");
                    return RedirectToAction("Logout", "Account");
                }
                Menegar? menegar = _context.Menegars.SingleOrDefault(m => m.IdSchool == student.IdSchool);

                var document = new StudentEnrollmentCertificate(
                    student?.Name ?? "غير معرف", student?.IdNumber ?? 0,
                    student?.IdClassNavigation?.Name ?? "غير معرف",
                    student?.IdSchoolNavigation?.Name ?? "غير معرف",
                    menegar?.Name ?? "لم يتم اعتماده بعد.");
                var stream = new MemoryStream();
                document.GeneratePdf(stream);
                stream.Position = 0;

                return File(stream, "application/pdf", $"شهادة_قيد_{student?.Name ?? "غير معرف"}.pdf");

            }
            catch (Exception ex)
            {
                _logger.LogAsync(ex, "Student/DownloadStudentCertificate");
                _notyf.Error("حدث خطا اثناء انشاء شهادة قيد.\nيرجى المحاولة لاحقا");
                if (HttpContext.Session.GetString("Role") == "Student")
                    return View(nameof(Index));
                return RedirectToAction("Details", "Student");
            }
        }

        [HttpGet("ChangeClass")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> GetChangeClass(Guid? idStudent)
        {
            var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "StudentApi/PostCreateStudent");
            
            if (!isValid)
            {
                return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
            }

            if (idStudent == null)
            {
                await _logger.LogAsync(new Exception("ارسال معرف فارغ"), "StudentApi/GetChangeClass");
                return BadRequest(new { success = false, message = "لا يمكن ارسال معرف فارغ" });
            }

            Guid Id;

            try
            {
                Id = idStudent ?? Guid.Empty;

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/GetChangeClass");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع" });
            }

            try
            {
                Student? student = await _context.Students
                    .Where(s => s.IdSchool == school &&  s.Id == Id && s.IsDeletedClass == false && s.IsDeletedStudent == false )
                    .FirstOrDefaultAsync();

                if (student == null)
                {
                    return BadRequest(new { success = false, message = "لا يمكن التلاعب بالبيانات المرسلة" });
                }

                List<theClass> theClass = await _context.TheClasses.Where(c => c.IdSchool == school && c.IsDeleted == false)
                .Select(c => new theClass
                {
                    idClass = c.Id,
                    nameClass = c.Name
                }).ToListAsync();
                if (!theClass.Any())
                {
                    return BadRequest(new { success = false, message = "لا يوجد صفوف متاحة حاليا" });
                }

                return Ok(new GetChangeClassStudent
                {
                    nameStudent = student.Name,
                    idStudent = student.Id,
                    lastIdClassEnc = student.IdClass ?? Guid.Empty,
                    theClasses = theClass,
                    lastIdClass = student.IdClass ?? Guid.Empty
                });
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/GetChangeClass");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع" });
            }

        }

        [HttpPost("ChangeClass")]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        public async Task<IActionResult> PostChangeClass([FromBody] PostChangeClassStudent student)
        {
            var (isValid, school, message) = await _sessionValidatorService
                .ValidateManagerSessionAsync(HttpContext, "StudentApi/PostChangeClass");
            if (!isValid)
                return Unauthorized(new ApiResponse { Success = false, Message = message });

            if (student.idClass == null || student.idStudent == null)
            {
                return BadRequest(new{success = false, message = "لا يمكن ارسال بيانات فارغة"});
            }

            Guid IdStudent;
            try
            {
                IdStudent = student.idStudent ?? Guid.Empty;
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/PostChangeClass");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع" });
            }

            try
            {
                Student? std = await _context.Students.FirstOrDefaultAsync(s =>
                    s.Id == IdStudent && s.IdSchool == school && !s.IsDeletedStudent);
                TheClass? classes = await _context.TheClasses.FirstOrDefaultAsync(c =>
                    c.Id == student.idClass && c.IdSchool == school && !c.IsDeleted);
                if (std == null || classes == null)
                {
                    return BadRequest(new { success = false, message = "لا يمكن التلاعب بالبيانات المرسلة" });
                }
                Console.WriteLine("===================================4");
                

                TeacherLectuerClass? teacherLectuerClass = await _context.TeacherLectuerClasses
                    .Where(sclt => sclt.IdClass == student.idClass && sclt.IdSchool == std.IdSchool && sclt.IsDeletedTeacher == false)
                    .FirstOrDefaultAsync();

                std.IdClass = student.idClass;

                List<Grade>? grade = await _context.Grades
                    .Where(g => g.IdStudent == IdStudent)
                    .ToListAsync();
                foreach (var item in grade)
                {
                    item.IdClass = student.idClass;
                    item.IdTeacher = teacherLectuerClass?.IdTeacher;
                }

                List<Attendance>? attendances = await _context.Attendances
                    .Where(g => g.IdStudent == IdStudent)
                    .ToListAsync();
                foreach (var item in attendances)
                {
                    item.IdTeacher = teacherLectuerClass?.IdTeacher;
                    item.IdClass = student.idClass;
                }

                List<StudentLectuerTeacher>? studentLectuerTeachers = await _context.StudentLectuerTeachers
                    .Where(g => g.IdStudent == IdStudent)
                    .ToListAsync();
                foreach (var item in studentLectuerTeachers)
                {
                    item.IdClass = student.idClass;
                    item.IdTeacher = teacherLectuerClass?.IdTeacher;
                }

                await _context.SaveChangesAsync();
                await _cache.RemoveAsync($"Students_School_{school}");
                return Ok();
                


            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "StudentApi/PostChangeClass");
                return StatusCode(500, new { success = false, message = "حدث خطأ غير متوقع" });
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

        private bool StudentExists(Guid id)
        {
            return _context.Students.Any(e => e.Id == id);
        }
    }
}

