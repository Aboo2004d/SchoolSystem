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
            if (teacherId != idTeacher || teacher != null)
            {
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

            if (studentId != idstudent)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة."), sours);
                return (false, 0, 0, true);
            }

            var student = await _context.Students.FindAsync(studentId);
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

    public async Task<(bool IsValid, int IdSchool, bool status)> ValidateAdminSessionAsync(HttpContext httpContext, string sours)
    {
        try
        {

            int? idmenegar = httpContext.Session.GetInt32("Id") ?? 0;


            if (idmenegar == 0 )
            {
                _notyf.Error("دخول غير مصرح به. انتهت صلاحية الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), sours);
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0, false);
            }
            Menegar? menegar = await _context.Menegars.FindAsync(idmenegar);

            if (menegar != null)
            {
                _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
                await _logger.LogAsync(new Exception("التلاعب بالبيانات المرسلة."), sours);
                return (false, 0, true);
            }

            int? idSchool = menegar?.IdSchool ?? 0;
            if (idSchool == 0)
            {
                _notyf.Error("دخول غير مصرح به. انتهت صلاحية الجلسة.");
                await _logger.LogAsync(new Exception("دخول غير مصرح."), sours);
                await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return (false, 0, false);
            }

            return (true, idSchool.Value, true);
        }
        catch (Exception ex)
        {
            _notyf.Error("حدث خطأ غير متوقع/nحاول لاحقا.");
            await _logger.LogAsync(ex, sours);
            await httpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return (false, 0, false);
        }
    }

}
