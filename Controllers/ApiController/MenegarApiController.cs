using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;
using SchoolSystem.Controllers;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Caching.Distributed;
using System.Globalization;
using SchoolSystem.Models.AdminSchool;

namespace SchoolSystem.Controllers
{
    [ApiController]
    [Route("/api/menegar")]
    public class MenegarApiController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly ISessionValidatorService _sessionValidatorService;
        private readonly IDistributedCache _cache;
        private readonly IAutomaticAccountService _accounts;
        


        public MenegarApiController(SystemSchoolDbContext context, IDistributedCache cache, ISessionValidatorService sessionValidatorService, INotyfService notyf, IErrorLoggerService logger, IAutomaticAccountService accounts)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _sessionValidatorService = sessionValidatorService;
            _cache = cache;
            _accounts = accounts;
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("MenegarStudent")]
        public async Task<IActionResult> ManagerMenegarStudent(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length,
            [FromQuery(Name = "search[value]")] string searchValue="")
        {
            try
            {
                // التحقق من صلاحية المستخدم
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "MenegarApi/ManagerMenegarStudent");
            
                if (!isValid)
                {
                    return Unauthorized(new ApiResponse { Success = isValid, Message = Message });
                }

                // فحص القيم المرسلة من الفرونت

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

                string studentsKey = $"Students_School_{school}";
                var cachedData = await _cache.GetStringAsync(studentsKey);
                Response.Headers["X-Cache"] = string.IsNullOrEmpty(cachedData) ? "MISS" : "HIT";
                Response.Headers["X-Cache-Key"] = studentsKey;
                List<StudentInSchool> students;
                IQueryable<StudentInSchool> AllStudent;

                if (string.IsNullOrEmpty(cachedData))
                {

                    // الاستعلام الأساسي مع تحسين الأداء
                    // Aggregate the large child tables once. The former navigation projection generated
                    // repeated correlated AVG/COUNT subqueries per student and timed out under load.
                    var gradeAverages = await _context.Grades.AsNoTracking()
                        .Where(g => g.IdSchool == school)
                        .GroupBy(g => g.IdStudent)
                        .Select(group => new
                        {
                            IdStudent = group.Key,
                            Average = group.Average(g => (double)g.Total)
                        })
                        .ToDictionaryAsync(item => item.IdStudent, item => item.Average);

                    var attendanceTotals = await _context.Attendances.AsNoTracking()
                        .Where(a => a.IdSchool == school)
                        .GroupBy(a => a.IdStudent)
                        .Select(group => new
                        {
                            IdStudent = group.Key,
                            TotalDays = group.Count(),
                            AttendedDays = group.Count(a => a.AttendanceStatus == "1" || a.AttendanceStatus == "m")
                        })
                        .ToDictionaryAsync(item => item.IdStudent);

                    var studentRows = await _context.Students.AsNoTracking()
                        .Where(std => std.IdSchool == school && !std.IsDeletedStudent)
                        .Select(std => new
                        {
                            IdStudent = std.Id,
                            NameStudent = std.Name,
                            NameClass = std.IdClassNavigation == null ? "فارغ"
                                : std.IdClassNavigation.IsDeleted
                                    ? (std.IdClassNavigation.Name ?? "") + " (صف محذوف)"
                                    : std.IdClassNavigation.Name
                        }).ToListAsync();

                    students = studentRows.Select(student => new StudentInSchool
                    {
                        idStudent = student.IdStudent,
                        nameStudent = student.NameStudent,
                        nameClass = student.NameClass,
                        average = (gradeAverages.TryGetValue(student.IdStudent, out var average) ? average : 0d)
                            .ToString("0.##", CultureInfo.InvariantCulture),
                        days = attendanceTotals.TryGetValue(student.IdStudent, out var attendance)
                            ? $"{attendance.AttendedDays} / {attendance.TotalDays}"
                            : "0 / 0"
                    }).ToList();

                    var options = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromDays(25)) // تعيين مدة الصلاحية
                        .SetSlidingExpiration(TimeSpan.FromDays(1)); // تعيين مدة التحديث

                    await _cache.SetStringAsync(
                        studentsKey,
                        System.Text.Json.JsonSerializer.Serialize(students),
                        options
                    );
                }
                else
                {
                    students = System.Text.Json.JsonSerializer.Deserialize<List<StudentInSchool>>(cachedData);
                }

                if( students == null)
                {
                    return BadRequest(new ApiResponse { Success = false, Message =  "لا توجد بيانات للطلاب" });
                }

                AllStudent = students.AsQueryable();
                int TotalStudents = students.Count;

                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    AllStudent = SearchInStudents(searchValue, AllStudent);
                }

                int TotalFilterStudents = AllStudent.Count();
    

                // الترتيب
                AllStudent = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => AllStudent.OrderBy(s => s.nameStudent),
                    ("0", "desc") => AllStudent.OrderByDescending(s => s.nameStudent),
                    ("1", "asc") => AllStudent.OrderBy(s => s.nameClass),
                    ("1", "desc") => AllStudent.OrderByDescending(s => s.nameClass),
                    ("2", "asc") => AllStudent.OrderBy(s => s.average),
                    ("2", "desc") => AllStudent.OrderByDescending(s => s.average),
                    ("3", "asc") => AllStudent.OrderBy(s => s.days),
                    ("3", "desc") => AllStudent.OrderByDescending(s => s.days),
                    _ => AllStudent.OrderBy(s => s.nameStudent)
                };

                //التقطيع
                var data = AllStudent
                .Skip(start)
                .Take(length)
                .ToList();



                //الحصول على القيم بعد الفلترة
                //ارسال البيانات للعرض
                var student = data.Select(std =>
                new StudentInSchool
                {
                    idStudent = std.idStudent,
                    nameStudent = std.nameStudent,
                    nameClass = std.nameClass,
                    average = std.average + "%",
                    days = std.days
                }).ToList();

                var result = new
                {
                    draw,
                    recordsTotal = TotalStudents,
                    recordsFiltered = TotalFilterStudents,
                    data = student
                };
                return Ok(result);

            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "MenegarApi/ManagerMenegarStudent");
                return StatusCode(500,new ApiResponse { Success = false, Message =  "حدث خطأ غير متوقع " });
            }
        }

        private IQueryable<StudentInSchool> SearchInStudents(string searchValue, IQueryable<StudentInSchool> AllData)
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

                        if (string.IsNullOrWhiteSpace(std.average))
                            return false;

                        // تنظيف النص وإزالة أي % وفراغات
                        string avgStr = std.average.Replace("%", "").Trim();

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
                // البحث النصي على الاسم، الصف، المعدل، الأيام
                return AllData.Where(std =>
                    (std.nameStudent != null && std.nameStudent.Contains(searchValue)) ||
                    (std.nameClass != null && std.nameClass.Contains(searchValue)) ||
                    (std.average != null && std.average.Contains(searchValue)) ||
                    (std.days != null && std.days.Contains(searchValue))
                );
            }
        }

        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("MenegarTeacher")]
        
        public async Task<IActionResult> ManagerMenegarTeacher(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length,
            [FromQuery(Name = "search[value]")] string searchValue="")
        {
            try
            {
                // التحقق من صلاحية المستخدم
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "MenegarApi/ManagerMenegarTeacher");
            
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


                string teachersKey = $"Teachers_School_{school}";
                var cachedData = await _cache.GetStringAsync(teachersKey);
                Response.Headers["X-Cache"] = string.IsNullOrEmpty(cachedData) ? "MISS" : "HIT";
                Response.Headers["X-Cache-Key"] = teachersKey;
                List<TeachersInSchool> teachers;
                IQueryable<TeachersInSchool> AllTeachers;

                if (string.IsNullOrEmpty(cachedData))
                {

                    // الاستعلام الأساسي مع تحسين الأداء
                    teachers = _context.Teachers.Where(Teach => Teach.IdSchool == school && Teach.IsDeleted == false)
                    .AsNoTracking()
                    .Select(t => new TeachersInSchool
                    {
                        idTeacher = t.Id,
                        nameTeacher = t.Name,
                        phoneTeacher = t.Phone,
                        emailAddressTeacher = t.Email

                    }).ToList();

                    var options = new DistributedCacheEntryOptions()
                        .SetAbsoluteExpiration(TimeSpan.FromDays(25)) // تعيين مدة الصلاحية
                        .SetSlidingExpiration(TimeSpan.FromDays(1)); // تعيين مدة التحديث

                    await _cache.SetStringAsync(
                        teachersKey,
                        System.Text.Json.JsonSerializer.Serialize(teachers),
                        options
                    );
                }
                else
                {
                    teachers = System.Text.Json.JsonSerializer.Deserialize<List<TeachersInSchool>>(cachedData);
                }

                if( teachers == null)
                {
                    return BadRequest(new ApiResponse { Success = false, Message =  "لا توجد بيانات للمعلمين" });
                }

                AllTeachers = teachers.AsQueryable();
                var TotalTeachers = teachers.Count;


               // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    AllTeachers = SearchInTeachers(searchValue, AllTeachers);
                
                }

                // إجمالي عدد السجلات بعد الفلترة
                var TotalFilterTeacher = AllTeachers.Count();

                // الترتيب
                AllTeachers = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => AllTeachers.OrderBy(s => s.nameTeacher),
                    ("0", "desc") => AllTeachers.OrderByDescending(s => s.nameTeacher),
                    ("1", "asc") => AllTeachers.OrderBy(s => s.emailAddressTeacher),
                    ("1", "desc") => AllTeachers.OrderByDescending(s => s.emailAddressTeacher),
                    ("2", "asc") => AllTeachers.OrderBy(s => s.phoneTeacher),
                    ("2", "desc") => AllTeachers.OrderByDescending(s => s.phoneTeacher),
                    _ => AllTeachers.OrderBy(s => s.nameTeacher)
                };

                //تقطيع
                var data = AllTeachers
                        .Skip(start)
                        .Take(length)
                        .ToList();


                // ارسال بيانات للعرض
                var teacher = data.
                Select(s => new TeachersInSchool
                {
                    idTeacher = s.idTeacher,
                    nameTeacher = s.nameTeacher,
                    emailAddressTeacher = s.emailAddressTeacher,
                    phoneTeacher = s.phoneTeacher
                }).ToList();

                var result = new
                {
                    draw,
                    recordsTotal = TotalTeachers,
                    recordsFiltered = TotalFilterTeacher,
                    data = teacher
                };


                return Ok(result);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "MenegarApi/ManagerMenegarTeacher");
                return StatusCode(500,new ApiResponse { Success = false, Message =  "حدث خطأ غير متوقع" });
                
            }
        }

        private IQueryable<TeachersInSchool> SearchInTeachers(string searchValue, IQueryable<TeachersInSchool> AllData)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
                return AllData;

            // السماح فقط بالحروف، الأرقام، المسافات، < > = / .
            searchValue = Regex.Replace(searchValue, @"[^\w\s@.-_]", ""); 

             // البحث النصي 
            return AllData.Where(teah =>
                (teah.nameTeacher != null && teah.nameTeacher.Contains(searchValue)) ||
                (teah.phoneTeacher != null && teah.phoneTeacher.Contains(searchValue)) ||
                (teah.emailAddressTeacher != null && teah.emailAddressTeacher.Contains(searchValue))
            );
            
        }


        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("MenegarClass")]
        public async Task<IActionResult> ManagerMenegarClass(
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length,
            [FromQuery(Name = "search[value]")] string searchValue="")
        {
            try
            {
                // التحقق من صلاحية المستخدم
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "MenegarApi/ManagerMenegarClass");
            
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

                // الاستعلام الأساسي مع تحسين الأداء
                var Std_Teach_InClass = _context.TheClasses.Where(std => std.IdSchool == school && std.IsDeleted == false)
                    .AsNoTracking()
                    .Select(s => new StudentsTeachersInClassAtSchool
                    {
                        idClass = s.Id,
                        nameClass = s.Name,
                        numberOfStudents = s.Students.Where(std => std.IdClass == s.Id && s.IdSchool == school && std.IsDeletedStudent == false)
                        .Select(sc => sc.Id).Distinct().Count(),
                        numberOfTeachers = s.TeacherLectuerClasses.Where(std => std.IdClass == s.Id && s.IdSchool == school && std.IsDeletedTeacher == false && std.IdTeacherNavigation.IsDeleted == false)
                        .Select(sc => sc.IdTeacher).Distinct().Count()

                    });

                var TotalClasses = await Std_Teach_InClass.CountAsync();

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    Std_Teach_InClass = SearchInDataClass(searchValue, Std_Teach_InClass);
                }

                var TotalFilterClasses = await Std_Teach_InClass.CountAsync();

                // الترتيب
                Std_Teach_InClass = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => Std_Teach_InClass.OrderBy(s => s.nameClass),
                    ("0", "desc") => Std_Teach_InClass.OrderByDescending(s => s.nameClass),
                    ("1", "asc") => Std_Teach_InClass.OrderBy(s => s.numberOfStudents),
                    ("1", "desc") => Std_Teach_InClass.OrderByDescending(s => s.numberOfStudents),
                    ("2", "asc") => Std_Teach_InClass.OrderBy(s => s.numberOfTeachers),
                    ("2", "desc") => Std_Teach_InClass.OrderByDescending(s => s.numberOfTeachers),
                    _ => Std_Teach_InClass.OrderBy(s => s.nameClass)
                };

                var theClasses = await Std_Teach_InClass
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                var Student_Teacher_InClass = theClasses.
                Select(s => new StudentsTeachersInClassAtSchool
                {
                    idClass = s.idClass,
                    nameClass = s.nameClass,
                    numberOfStudents = s.numberOfStudents,
                    numberOfTeachers = s.numberOfTeachers
                })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = TotalClasses,
                    recordsFiltered = TotalFilterClasses,
                    data = Student_Teacher_InClass
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "MenegarApi/ManagerMenegarClass");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطا غير متوقع" });
            }
        }

        private IQueryable<StudentsTeachersInClassAtSchool> SearchInDataClass(string searchValue, IQueryable<StudentsTeachersInClassAtSchool> AllData)
        {
            if (string.IsNullOrWhiteSpace(searchValue))
                return AllData;

            // السماح فقط بالحروف، الأرقام، المسافات، < > = / .
            searchValue = Regex.Replace(searchValue, @"[^\w\s@.-_]", ""); 

             // البحث النصي 
            return AllData.Where(s =>
                        (s.nameClass != null && s.nameClass.Contains(searchValue))||
                        (s.numberOfStudents.ToString() != null && s.numberOfStudents.ToString().Contains(searchValue))||
                        (s.numberOfTeachers.ToString() != null && s.numberOfTeachers.ToString().Contains(searchValue))
                    );
            
        }


        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("MenegarStudentInClass")]
        public async Task<IActionResult> ManagerMenegarStudentInClass(
            Guid idClass,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length,
            [FromQuery(Name = "search[value]")] string searchValue = "")
        {
            try
            {
                // التحقق من صلاحية المستخدم
                var (isValid, school, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "MenegarApi/ManagerMenegarStudentInClass");
            
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
                    await _logger.LogAsync(ex, "MenegarApi/ManagerMenegarStudentInClass");
                    return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
                }


                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;

                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.Students.Where(std => std.IdSchool == school && std.IdClass == Id && std.IsDeletedStudent == false)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var AllStudentInClass = _context.Students.Where(std => std.IdSchool == school && std.IdClass == Id && std.IsDeletedStudent == false)
                    .AsNoTracking()
                    .Select(s => new StudentsInClassAtSchool
                    {
                        idStudent = s.Id,
                        nameStudent = s.Name,
                        nameClass = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "غير معروف",
                        avg = s.Grades.Select(g => g.Total).Average() == null ? "0%" : s.Grades.Select(g => g.Total).Average() + "%"
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    AllStudentInClass = SearchInStudentAtClass(searchValue, AllStudentInClass);
                }

                // الترتيب
                AllStudentInClass = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => AllStudentInClass.OrderBy(s => s.nameStudent),
                    ("0", "desc") => AllStudentInClass.OrderByDescending(s => s.nameStudent),
                    ("1", "asc") => AllStudentInClass.OrderBy(s => s.nameClass),
                    ("1", "desc") => AllStudentInClass.OrderByDescending(s => s.nameClass),
                    ("2", "asc") => AllStudentInClass.OrderBy(s => s.avg),
                    ("2", "desc") => AllStudentInClass.OrderByDescending(s => s.avg),
                    _ => AllStudentInClass.OrderBy(s => s.nameStudent)
                };

                // الحصول على القيم بعد الفلترة
                var TotalStudentInClasses = await AllStudentInClass.CountAsync();

                // تقطيع
                var studentClass = await AllStudentInClass
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // ارسال بيانات للعرض
                var StudentInClass = studentClass.
                Select(s => new StudentsInClassAtSchool
                {
                    idStudent = s.idStudent,
                    nameClass = s.nameClass,
                    nameStudent = s.nameStudent,
                    avg = s.avg

                }).ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = TotalStudentInClasses,
                    data = StudentInClass
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                await _logger.LogAsync(ex, "MenegarApi/ManagerMenegarStudentInClass");
                return StatusCode(500, new ApiResponse { Success = false, Message = "حدث خطأ غير متوقع" });
            }
        }

        private IQueryable<StudentsInClassAtSchool> SearchInStudentAtClass(string searchValue, IQueryable<StudentsInClassAtSchool> AllData)
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

                        if (string.IsNullOrWhiteSpace(std.avg))
                            return false;

                        // تنظيف النص وإزالة أي % وفراغات
                        string avgStr = std.avg.Replace("%", "").Trim();

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
                 // البحث النصي على الاسم، الصف، المعدل، الأيام
                return AllData.Where(s =>
                    (s.nameClass != null && s.nameClass.Contains(searchValue))||
                    (s.avg != null && s.avg.Contains(searchValue))||
                    (s.nameStudent != null && s.nameStudent.Contains(searchValue))
                );
            }

            
            
        }


        [HttpGet]
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        [HttpGet("MenegarTeacherInClass")]
        public async Task<JsonResult> ManagerMenegarTeacherInClass(
            Guid idClass,
            [FromQuery] int draw,
            [FromQuery] int start,
            [FromQuery] int length = 10,
            [FromQuery(Name = "search[value]")] string searchValue = "")

        {
            try
            {
                Guid Id;


                // التحقق من صلاحية المستخدم و التلاعب بالبيانات
                var (IsValid, IdSchool, Message) = await _sessionValidatorService.ValidateManagerSessionAsync(HttpContext, "Lectuer/ManagerMenegarClass");
                if (!IsValid)
                {
                    return Json(new { success = false, message = Message});
                }

                try
                {
                    Id = idClass;

                }
                catch (Exception ex)
                {
                    await _logger.LogAsync(ex, "Menegar/ManagerMenegarStudentInClassView");
                    return Json(new { success = false, message = Message});
                }

                // تعيين قيمة افتراضية اذا لم يتم ارسال القيمة
                if (length <= 0)
                    length = 10;

                // الحصول على القيم المرسلة
                var orderColumnIndex = Request.Query["order[0][column]"].ToString();
                var orderDir = Request.Query["order[0][dir]"].ToString().ToLower();

                // تعيين افتراضي في حالة القيم غير صالحة
                if (string.IsNullOrEmpty(orderColumnIndex)) orderColumnIndex = "0";
                if (string.IsNullOrEmpty(orderDir)) orderDir = "asc";

                // إجمالي عدد السجلات بدون فلترة
                var totalRecords = await _context.TeacherLectuerClasses
                .Where(std => std.IdSchool == IdSchool && std.IdClass == Id &&
                    std.IsDeletedTeacher == false && std.IdTeacherNavigation != null &&
                    std.IdTeacherNavigation.IsDeleted == false)
                .CountAsync();

                // الاستعلام الأساسي مع تحسين الأداء
                var query = _context.TeacherLectuerClasses.Where(tlc => tlc.IdSchool == IdSchool && tlc.IdClass == Id && tlc.IdTeacherNavigation.IsDeleted == false && tlc.IsDeletedTeacher == false)
                    .AsNoTracking()
                    .Select(s => new
                    {
                        Id = s.Id,
                        IdTeacher = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Id : Guid.Empty,
                        IdClass = s.IdClassNavigation != null ? s.IdClassNavigation.Id : Guid.Empty,
                        IdLectuer = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Id : Guid.Empty,
                        TeacherName = s.IdTeacherNavigation != null ? s.IdTeacherNavigation.Name : "Unknown",
                        LectuerName = s.IdLectuerNavigation != null ? s.IdLectuerNavigation.Name : "Unknown",
                        ClassroomName = s.IdClassNavigation != null ? s.IdClassNavigation.Name : "Unknown"
                    });

                // البحث
                if (!string.IsNullOrWhiteSpace(searchValue))
                {
                    query = query.Where(s =>
                        (s.TeacherName != null && s.TeacherName.Contains(searchValue)) ||
                        (s.ClassroomName != null && s.ClassroomName.Contains(searchValue)) ||
                        (s.LectuerName != null && s.LectuerName.Contains(searchValue))
                    );
                }

                // الحصول على القيم بعد الفلترة
                var filteredCount = await query.CountAsync();

                // الترتيب
                query = (orderColumnIndex, orderDir) switch
                {
                    ("0", "asc") => query.OrderBy(s => s.TeacherName),
                    ("0", "desc") => query.OrderByDescending(s => s.TeacherName),
                    ("1", "asc") => query.OrderBy(s => s.ClassroomName),
                    ("1", "desc") => query.OrderByDescending(s => s.ClassroomName),
                    ("2", "asc") => query.OrderBy(s => s.LectuerName),
                    ("2", "desc") => query.OrderByDescending(s => s.LectuerName),
                    _ => query.OrderBy(s => s.TeacherName)
                };

                // تقطيع
                var data = await query
                        .Skip(start)
                        .Take(length)
                        .ToListAsync();

                // ارسال البيانات للعرض
                var teacher = data.
                Select(s => new ManagerMenegarTeacherInClassViewModel
                {
                    Id = s.Id,
                    ClassroomName = s.ClassroomName,
                    TeacherName = s.TeacherName,
                    LectuerName = s.LectuerName,
                    IdClass = s.IdClass,
                    IdTeacher = s.IdTeacher,
                    IdLectuer = s.IdLectuer
                })
                    .ToList();

                var result = new
                {
                    draw,
                    recordsTotal = totalRecords,
                    recordsFiltered = filteredCount,
                    data = teacher
                };

                return Json(result);
            }
            catch (Exception e)
            {
                await _logger.LogAsync(e, "Menegar/ManagerMenegarTeacherInClass");
                _notyf.Error("حدث خطا غير متوقع\nيرجى المحاولة لاحقا");
                return Json(new { message = e.Message, stack = e.StackTrace });
            }
        }

        [HttpGet]
        public JsonResult GetStudentCountPerClass()
        {
            Console.WriteLine("---------------------------------------");
            var data = _context.TheClasses.Where(c => c.IdSchool == HttpContext.Session.GetGuid("School") && c.IsDeleted == false )
                .Select(c => new {
                    ClassName = c.Name,
                    StudentCount = c.Students.Where(sc => sc.IsDeletedStudent == false).Count()
                }).ToList();
                Console.WriteLine("---------------------------------------");
                Console.WriteLine($"Count: {data.Count()}");

            return Json(data);
        }

        [HttpGet("CountTeacherPerSubject")]
        public JsonResult GetTeacherCountPerSubject()
        {
            Console.WriteLine("---------------------------------------123");
            var data = _context.TeacherLectuerClasses.Where(t => t.IdSchool == HttpContext.Session.GetGuid("School") && t.IdLectuerNavigation.IsDeleted == false)
                .Include(t => t.IdLectuerNavigation) // تأكد من تضمين المادة
                .GroupBy(t => t.IdLectuerNavigation.Name)
                .Select(g => new
                {
                    subject = g.Key,
                    teacherCount = g.Where(x => x.IsDeletedTeacher == false).Select(x => x.IdTeacher).Distinct().Count()
                })
                .ToList();
            Console.WriteLine($"CountTeacher: {data.Count()}");
            foreach (var x in data)
            {
                Console.WriteLine($"Subject: {x.subject}, TeacherCount: {x.teacherCount}");
            }

            Console.WriteLine("===============================================================");
                
            return Json(data);
        }

        [HttpPost("reset-student-password")]
        [AuthorizeRoles(RoleNames.Manager)]
        public async Task<IActionResult> ResetStudentPassword([FromBody] ResetStudentPasswordRequest request)
        {
            if (!int.TryParse(request.IdentityNumber, out var identityNumber)) return BadRequest(new ApiResponse { Success = false, Message = "رقم الهوية غير صالح." });
            var userId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result = await _accounts.ResetStudentPasswordAsync(userId, identityNumber);
            return result.Success ? Ok(new ApiResponse { Success = true, Message = result.Message }) : NotFound(new ApiResponse { Success = false, Message = result.Message });
        }
        [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
        private bool MenegarExists(Guid id)
        {
            return _context.Menegars.Any(e => e.Id == id);
        }

    }
}

