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

    public async Task<(bool IsValid, Guid IdTeacher, Guid IdSchool, bool status)> ValidateTeacherSessionAsync(
        HttpContext httpContext, Guid teacherId, string source)
    {
        var user = await GetAuthorizedUserAsync(httpContext, RoleNames.Teacher, source);
        if (user is null) return (false, Guid.Empty, Guid.Empty, false);
        var profile = await _context.Teachers.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted &&
                x.IdSchoolNavigation != null && x.IdSchoolNavigation.IsActive && !x.IdSchoolNavigation.IsDeleted);
        if (profile is null || profile.Id != teacherId || !profile.IdSchool.HasValue)
            return await RejectAsync(source, (false, Guid.Empty, Guid.Empty, true));
        return (true, profile.Id, profile.IdSchool.Value, true);
    }

    public async Task<(bool IsValid, Guid IdTeacher, Guid IdSchool, bool status)> ValidateStudentSessionAsync(
        HttpContext httpContext, Guid studentId, string source)
    {
        var user = await GetAuthorizedUserAsync(httpContext, RoleNames.Student, source);
        if (user is null) return (false, Guid.Empty, Guid.Empty, false);
        var profile = await _context.Students.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeletedStudent &&
                x.IdSchoolNavigation != null && x.IdSchoolNavigation.IsActive && !x.IdSchoolNavigation.IsDeleted);
        if (profile is null || profile.Id != studentId || !profile.IdSchool.HasValue)
            return await RejectAsync(source, (false, Guid.Empty, Guid.Empty, true));
        return (true, profile.Id, profile.IdSchool.Value, true);
    }

    public async Task<(bool IsValid, Guid IdStudent, Guid IdSchool, bool status)> ValidateStudentDataAccessAsync(
        HttpContext httpContext, Guid studentId, string source)
    {
        var user = await _userManager.GetUserAsync(httpContext.User);
        if (user is null || !user.IsActive)
            return (false, Guid.Empty, Guid.Empty, false);

        var student = await _context.Students.AsNoTracking().SingleOrDefaultAsync(x =>
            x.Id == studentId && !x.IsDeletedStudent && !x.IsDeletedSchool && x.IdSchool.HasValue &&
            x.IdSchoolNavigation != null && x.IdSchoolNavigation.IsActive && !x.IdSchoolNavigation.IsDeleted);
        if (student is null)
            return await RejectAsync(source, (false, Guid.Empty, Guid.Empty, true));
        var targetSchoolId = student.IdSchool!.Value;

        if (await _userManager.IsInRoleAsync(user, RoleNames.Admin))
            return (true, student.Id, targetSchoolId, true);

        if (await _userManager.IsInRoleAsync(user, RoleNames.Student))
        {
            var ownsStudentProfile = student.ApplicationUserId == user.Id;
            return ownsStudentProfile
                ? (true, student.Id, targetSchoolId, true)
                : await RejectAsync(source, (false, Guid.Empty, Guid.Empty, true));
        }

        if (await _userManager.IsInRoleAsync(user, RoleNames.Manager))
        {
            var managerSchoolId = await _context.Menegars.AsNoTracking()
                .Where(manager => manager.ApplicationUserId == user.Id && !manager.IsDeleted &&
                    !manager.IsDeletedSchool)
                .Select(manager => manager.IdSchool)
                .SingleOrDefaultAsync();

            return managerSchoolId.HasValue && managerSchoolId.Value == targetSchoolId
                ? (true, student.Id, targetSchoolId, true)
                : await RejectAsync(source, (false, Guid.Empty, Guid.Empty, true));
        }

        await _logger.LogAsync(new UnauthorizedAccessException("Role is not allowed to access student data."), source);
        return (false, Guid.Empty, Guid.Empty, true);
    }

    public async Task<(bool IsValid, Guid IdSchool, string Message)> ValidateManagerSessionAsync(
        HttpContext httpContext, string source)
    {
        var user = await GetAuthorizedUserAsync(httpContext, RoleNames.Manager, source);
        if (user is null) return (false, Guid.Empty, "انتهت صلاحية تسجيل الدخول.");
        var profile = await _context.Menegars.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted &&
                x.IdSchoolNavigation != null && x.IdSchoolNavigation.IsActive && !x.IdSchoolNavigation.IsDeleted);
        if (profile is null || !profile.IdSchool.HasValue)
            return await RejectAsync(source, (false, Guid.Empty, "ملف المدير غير صالح."));
        return (true, profile.IdSchool.Value, string.Empty);
    }

    public async Task<(bool IsValid, Guid DirectorateId, string Message)> ValidateDirectorateManagerSessionAsync(
        HttpContext httpContext, string source)
    {
        var user = await GetAuthorizedUserAsync(httpContext, RoleNames.DirectorateManager, source);
        if (user is null) return (false, Guid.Empty, "انتهت صلاحية تسجيل الدخول.");
        var profile = await _context.DirectorateManagers.AsNoTracking()
            .Where(x => x.ApplicationUserId == user.Id && !x.IsDeleted && x.Directorate.IsActive)
            .Select(x => new { x.DirectorateId })
            .SingleOrDefaultAsync();
        return profile is null
            ? await RejectAsync(source, (false, Guid.Empty, "ملف مسؤول المديرية غير صالح."))
            : (true, profile.DirectorateId, string.Empty);
    }

    public async Task<(bool IsValid, Guid DirectorateId, string Message)> ValidateDirectorateSchoolAccessAsync(
        HttpContext httpContext, Guid schoolId, string source)
    {
        var access = await ValidateDirectorateManagerSessionAsync(httpContext, source);
        if (!access.IsValid) return access;
        var ownsSchool = await _context.Schools.AsNoTracking().AnyAsync(x =>
            x.Id == schoolId && x.DirectorateId == access.DirectorateId && !x.IsDeleted);
        return ownsSchool
            ? access
            : await RejectAsync(source, (false, Guid.Empty, "المدرسة لا تتبع لهذه المديرية."));
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
