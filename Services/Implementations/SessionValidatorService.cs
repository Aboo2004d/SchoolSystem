using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;

public sealed class SessionValidatorService : ISessionValidatorService
{
    private readonly SystemSchoolDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IErrorLoggerService _logger;
    private readonly INotyfService _notyf;

    public SessionValidatorService(SystemSchoolDbContext context, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, IErrorLoggerService logger, INotyfService notyf)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _logger = logger;
        _notyf = notyf;
    }

    public async Task<(bool IsValid, int IdTeacher, int IdSchool, bool status)> ValidateTeacherSessionAsync(
        HttpContext httpContext, int teacherId, string source)
    {
        var user = await GetAuthorizedUserAsync(httpContext, RoleNames.Teacher, source);
        if (user is null) return (false, 0, 0, false);
        var profile = await _context.Teachers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted);
        if (profile is null || profile.Id != teacherId || !profile.IdSchool.HasValue)
            return await RejectAsync(source, (false, 0, 0, true));
        return (true, profile.Id, profile.IdSchool.Value, true);
    }

    public async Task<(bool IsValid, int IdTeacher, int IdSchool, bool status)> ValidateStudentSessionAsync(
        HttpContext httpContext, int studentId, string source)
    {
        var user = await GetAuthorizedUserAsync(httpContext, RoleNames.Student, source);
        if (user is null) return (false, 0, 0, false);
        var profile = await _context.Students.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeletedStudent);
        if (profile is null || profile.Id != studentId || !profile.IdSchool.HasValue)
            return await RejectAsync(source, (false, 0, 0, true));
        return (true, profile.Id, profile.IdSchool.Value, true);
    }

    public async Task<(bool IsValid, int IdSchool, string Message)> ValidateAdminSessionAsync(
        HttpContext httpContext, string source)
    {
        var user = await GetAuthorizedUserAsync(httpContext, RoleNames.Admin, source);
        if (user is null) return (false, 0, "انتهت صلاحية تسجيل الدخول.");
        var profile = await _context.Menegars.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted);
        if (profile is null || !profile.IdSchool.HasValue)
            return await RejectAsync(source, (false, 0, "ملف المدير غير صالح."));
        return (true, profile.IdSchool.Value, string.Empty);
    }

    private async Task<ApplicationUser?> GetAuthorizedUserAsync(HttpContext context, string role, string source)
    {
        var user = await _userManager.GetUserAsync(context.User);
        if (user is not null && user.IsActive && await _userManager.IsInRoleAsync(user, role))
            return user;
        await _logger.LogAsync(new UnauthorizedAccessException("Identity authorization failed."), source);
        await _signInManager.SignOutAsync();
        return null;
    }

    private async Task<T> RejectAsync<T>(string source, T result)
    {
        _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة.");
        await _logger.LogAsync(new UnauthorizedAccessException("Profile identifier mismatch."), source);
        return result;
    }
}
