using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SchoolSystem.Data;

public class SessionValidatorService : ISessionValidatorService
{
    private readonly SystemSchoolDbContext _context;
    private readonly IErrorLoggerService _logger;
    private readonly INotyfService _notyf;

    public SessionValidatorService(SystemSchoolDbContext context, IErrorLoggerService logger, INotyfService notyf)
    {
        _context = context;
        _logger = logger;
        _notyf = notyf;
    }

    public async Task<(bool IsValid, int IdTeacher, int IdSchool, bool status)> ValidateTeacherSessionAsync(HttpContext httpContext, int teacherId, string sours)
    {
        try
        {

            int? idTeacher = httpContext.Session.GetInt32("Id") ?? 0;

            if (idTeacher == 0)
            {
                _notyf.Error("دخول غير مصرح به. انتهت صلاحية الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), sours);
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0, 0, false);
            }

            var teacher = await _context.Teachers.FindAsync(idTeacher);
            if (teacherId != idTeacher || teacher == null)
            {
                Console.WriteLine($"TeacherId: {teacherId}, IdTeacher session: {idTeacher}");
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة."), sours);
                return (false, 0, 0, true);
            }

            int? idSchool = teacher?.IdSchool ?? 0;
            if (idSchool == 0)
            {
                _notyf.Error("دخول غير مصرح به. انتهت صلاحية الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), sours);
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0, 0, false);
            }

            return (true, idTeacher.Value, idSchool.Value, true);
        }
        catch (Exception ex)
        {
            _notyf.Error("حدث خطأ غير متوقع/nحاول لاحقا.");
            await _logger.LogAsync(ex, sours);
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            
            return (false, 0, 0, false);
        }
    }

    public async Task<(bool IsValid, int IdTeacher, int IdSchool, bool status)> ValidateStudentSessionAsync(HttpContext httpContext, int studentId, string sours)
    {
        try
        {

            int? idstudent = httpContext.Session.GetInt32("Id") ?? 0;

            if (idstudent == 0)
            {
                _notyf.Error("دخول غير مصرح به. انتهت صلاحية الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), sours);
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0, 0, false);
            }
            var student = await _context.Students.FindAsync(studentId);

            if (studentId != idstudent || student == null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة."), sours);
                return (false, 0, 0, true);
            }

            int? idSchool = student?.IdSchool ?? 0;
            if (idSchool == 0)
            {
                _notyf.Error("دخول غير مصرح به. انتهت صلاحية الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), sours);
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0, 0, false);
            }

            return (true, idstudent.Value, idSchool.Value, true);
        }
        catch (Exception ex)
        {
            _notyf.Error("حدث خطأ غير متوقع/nحاول لاحقا.");
            await _logger.LogAsync(ex, sours);
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return (false, 0, 0, false);
        }
    }

    public async Task<(bool IsValid, int IdSchool,string Message)> ValidateAdminSessionAsync(HttpContext httpContext, string sours)
    {
        try
        {
            var school = httpContext.Session.GetInt32("School") ?? 0;

            if (school == 0)
            {
                await _logger.LogAsync(new Exception("دخول غير مصرح."), sours);
                httpContext.Session.Clear(); // مسح الجلسة
                // تسجيل الخروج باستخدام ملفات تعريف الارتباط
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0,"انتهت صلاحية الجلسة");
            }

            int? idmenegar = httpContext.Session.GetInt32("Id") ?? 0;

            if (idmenegar == 0)
            {
                await _logger.LogAsync(new Exception("دخول غير مصرح."), sours);
                httpContext.Session.Clear(); // مسح الجلسة
                // تسجيل الخروج باستخدام ملفات تعريف الارتباط
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0,"انتهت صلاحية الجلسة");
            }
            Menegar? menegar = await _context.Menegars.FindAsync(idmenegar);
            if (menegar == null)
            {
                await _logger.LogAsync(new Exception("التلاعب ببيانات المدير."), sours);
                httpContext.Session.Clear(); // مسح الجلسة
                // تسجيل الخروج باستخدام ملفات تعريف الارتباط
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0, "لا يمكن التلاعب ببيانات المدير");
            }

            if (menegar.IdSchool != school)
            {
                await _logger.LogAsync(new Exception("مدرسة المدير غير مطابقة لمدرسته في الجلسة مما يعني هناك تلاعب بالبيانات."), sours);
                httpContext.Session.Clear(); // مسح الجلسة
                // تسجيل الخروج باستخدام ملفات تعريف الارتباط
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0, "لا يمكن التلاعب بالبيانات المحفوظة في الجلسة");
            }

            return (true, school, "");
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(ex, sours);
            httpContext.Session.Clear(); // مسح الجلسة
                // تسجيل الخروج باستخدام ملفات تعريف الارتباط
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return (false, 0, "حدث خطأ غير متوقع");
        }
    }

}
