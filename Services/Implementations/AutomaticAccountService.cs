using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;

public sealed class AutomaticAccountService : IAutomaticAccountService
{
    private readonly SystemSchoolDbContext _db;
    private readonly UserManager<ApplicationUser> _users;
    private readonly RoleManager<IdentityRole<Guid>> _roles;
    public AutomaticAccountService(SystemSchoolDbContext db, UserManager<ApplicationUser> users, RoleManager<IdentityRole<Guid>> roles)
        => (_db, _users, _roles) = (db, users, roles);

    public static string DefaultPassword(int identityNumber) => $"{identityNumber}@Aa";
    public static string PlaceholderEmail(int identityNumber) => $"{identityNumber}@users.schoolsystem.local";

    public async Task<(bool Success, string Message, ApplicationUser? User, string UserName, string InitialPassword, string Email)> CreateAsync(
        int identityNumber, string? email, string role, string? userName = null, string? password = null)
    {
        var resolvedUserName = string.IsNullOrWhiteSpace(userName) ? identityNumber.ToString() : userName.Trim();
        var resolvedPassword = string.IsNullOrWhiteSpace(password) ? DefaultPassword(identityNumber) : password;
        var resolvedEmail = string.IsNullOrWhiteSpace(email) ? PlaceholderEmail(identityNumber) : email.Trim();
        if (!await _roles.RoleExistsAsync(role)) return (false, "الدور المطلوب غير مهيأ.", null, resolvedUserName, resolvedPassword, resolvedEmail);
        if (await _users.FindByNameAsync(resolvedUserName) is not null) return (false, "اسم المستخدم مستخدم مسبقًا.", null, resolvedUserName, resolvedPassword, resolvedEmail);
        if (await _users.FindByEmailAsync(resolvedEmail) is not null) return (false, "البريد الإلكتروني مستخدم مسبقًا.", null, resolvedUserName, resolvedPassword, resolvedEmail);
        var user = new ApplicationUser { Id = Guid.NewGuid(), UserName = resolvedUserName, Email = resolvedEmail, EmailConfirmed = false, IsActive = true };
        var created = await _users.CreateAsync(user, resolvedPassword);
        if (!created.Succeeded) return (false, string.Join(" ", created.Errors.Select(x => x.Description)), null, resolvedUserName, resolvedPassword, resolvedEmail);
        var roleResult = await _users.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded) { await _users.DeleteAsync(user); return (false, string.Join(" ", roleResult.Errors.Select(x => x.Description)), null, resolvedUserName, resolvedPassword, resolvedEmail); }
        return (true, "تم إنشاء الحساب.", user, resolvedUserName, resolvedPassword, resolvedEmail);
    }

    public async Task<(bool Success, string Message)> ResetStudentPasswordAsync(Guid managerUserId, int studentIdentityNumber)
    {
        var schoolId = await _db.Menegars.AsNoTracking().Where(x => x.ApplicationUserId == managerUserId && !x.IsDeleted && !x.IsDeletedSchool).Select(x => x.IdSchool).SingleOrDefaultAsync();
        if (!schoolId.HasValue) return (false, "حساب مدير المدرسة غير صالح.");
        var student = await _db.Students.Include(x => x.ApplicationUser).SingleOrDefaultAsync(x => x.IdNumber == studentIdentityNumber && x.IdSchool == schoolId && !x.IsDeletedStudent && !x.IsDeletedSchool);
        if (student?.ApplicationUser is null) return (false, "لا يوجد طالب فعال بهذا الرقم في مدرستك.");
        var token = await _users.GeneratePasswordResetTokenAsync(student.ApplicationUser);
        var result = await _users.ResetPasswordAsync(student.ApplicationUser, token, DefaultPassword(studentIdentityNumber));
        if (!result.Succeeded) return (false, string.Join(" ", result.Errors.Select(x => x.Description)));
        await _users.ResetAccessFailedCountAsync(student.ApplicationUser);
        return (true, "تمت إعادة كلمة مرور الطالب إلى الكلمة الافتراضية.");
    }
}
