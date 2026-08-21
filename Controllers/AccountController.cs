using System.Security.Claims;
using AspNetCoreHero.ToastNotification.Abstractions;
using MailKit.Net.Smtp;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MimeKit;
using SchoolSystem.Data;
using SchoolSystem.Models;

namespace SchoolSystem.Controllers;

public class AccountController : Controller
{
    private readonly SystemSchoolDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly INotyfService _notyf;
    private readonly IAccountService _accountService;

    public AccountController(
        SystemSchoolDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        INotyfService notyf,
        IAccountService accountService)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _notyf = notyf;
        _accountService = accountService;
    }

    public bool CheckUser() => User.Identity?.IsAuthenticated == true;

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Login() => CheckUser() ? RedirectToAction("Index", "Home") : View();

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByNameAsync(model.UserName);
        if (user is null || !user.IsActive)
        {
            _notyf.Error("اسم المستخدم أو كلمة المرور غير صحيحة.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, false, lockoutOnFailure: true);
        if (result.IsLockedOut)
        {
            _notyf.Error("تم قفل الحساب مؤقتًا بسبب محاولات الدخول الفاشلة.");
            return View(model);
        }
        if (!result.Succeeded)
        {
            _notyf.Error("اسم المستخدم أو كلمة المرور غير صحيحة.");
            return View(model);
        }

        if (!await PopulateLegacySessionAsync(user))
        {
            await _signInManager.SignOutAsync();
            _notyf.Error("الحساب غير مرتبط بملف شخصي صالح.");
            return View(model);
        }

        var roleClaims = (await _userManager.GetRolesAsync(user))
            .Select(role => new Claim(ClaimTypes.Role, role));
        await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, roleClaims);

        _notyf.Success("تم تسجيل الدخول بنجاح.");
        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    [AllowAnonymous]
    [NonAction]
    public IActionResult Register() => CheckUser() ? RedirectToAction("Index", "Home") : View();

    [HttpPost]
    [AllowAnonymous]
    [NonAction]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        model.Role = RoleNames.Normalize(model.Role) ?? string.Empty;
        if (model.Role == RoleNames.DirectorateManager)
        {
            ModelState.AddModelError(nameof(model.Role), "حساب مسؤول المديرية يُنشأ إداريًا ولا يسمح بالتسجيل العام.");
            return View(model);
        }
        var validation = await _accountService.RegisterUserAsync(model);
        if (!validation.IsSuccess)
        {
            _notyf.Error(validation.Message);
            return View(model);
        }

        HttpContext.Session.SetString("PendingEmail", model.Email);
        HttpContext.Session.SetString("PendingRole", model.Role);
        HttpContext.Session.SetGuid("PendingProfileId", model.IdUser);
        HttpContext.Session.SetGuid("PendingSchool", model.School ?? Guid.Empty);
        HttpContext.Session.SetString("PendingName", model.FullName);
        return RedirectToAction(nameof(SetCredentials));
    }

    [HttpGet]
    [AllowAnonymous]
    [NonAction]
    public IActionResult SetCredentials()
    {
        if (CheckUser())
            return RedirectToAction("Index", "Home");

        var email = HttpContext.Session.GetString("PendingEmail");
        var role = HttpContext.Session.GetString("PendingRole");
        var profileId = HttpContext.Session.GetGuid("PendingProfileId");
        if (email is null || role is null || profileId is null)
            return RedirectToAction(nameof(Register));

        return View(new SetCredentialsViewModel
        {
            Email = email,
            Role = role,
            IdUser = profileId.Value,
            School = HttpContext.Session.GetGuid("PendingSchool") ?? Guid.Empty,
            name = HttpContext.Session.GetString("PendingName") ?? string.Empty
        });
    }

    [HttpPost]
    [AllowAnonymous]
    [NonAction]
    public async Task<IActionResult> SetCredentials(SetCredentialsViewModel model)
    {
        var pendingEmail = HttpContext.Session.GetString("PendingEmail");
        var pendingRole = RoleNames.Normalize(HttpContext.Session.GetString("PendingRole"));
        var pendingProfileId = HttpContext.Session.GetGuid("PendingProfileId");
        if (!ModelState.IsValid || pendingEmail is null || pendingRole is null || pendingProfileId is null)
            return View(model);

        if (!string.Equals(model.Email, pendingEmail, StringComparison.OrdinalIgnoreCase) ||
            model.IdUser != pendingProfileId.Value || RoleNames.Normalize(model.Role) != pendingRole)
            return BadRequest("بيانات التسجيل غير متطابقة.");

        if (!await _roleManager.RoleExistsAsync(pendingRole))
            return StatusCode(StatusCodes.Status500InternalServerError, "الدور المطلوب غير مهيأ.");

        await using var transaction = await _context.Database.BeginTransactionAsync();
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            UserName = model.UserName,
            Email = pendingEmail,
            EmailConfirmed = false,
            IsActive = true
        };
        var createResult = await _userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
        {
            AddIdentityErrors(createResult);
            return View(model);
        }

        var roleResult = await _userManager.AddToRoleAsync(user, pendingRole);
        if (!roleResult.Succeeded)
        {
            AddIdentityErrors(roleResult);
            return View(model);
        }

        if (!await LinkProfileAsync(user.Id, pendingRole, pendingProfileId.Value))
        {
            ModelState.AddModelError(string.Empty, "الملف الشخصي غير موجود أو مرتبط بحساب آخر.");
            return View(model);
        }

        await _context.SaveChangesAsync();
        await transaction.CommitAsync();
        await _signInManager.SignInAsync(user, isPersistent: false);
        await PopulateLegacySessionAsync(user);
        ClearPendingRegistration();
        _notyf.Success("تم إنشاء الحساب بنجاح.");
        return RedirectToAction("Index", "Home");
    }

    [HttpPost]
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear();
        await _signInManager.SignOutAsync();
        _notyf.Success("تم تسجيل الخروج.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    [NonAction]
    public IActionResult ForgotPassword() => CheckUser() ? RedirectToAction("Index", "Home") : View();

    [HttpPost]
    [AllowAnonymous]
    [NonAction]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email);
        if (user is not null && user.IsActive)
        {
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetLink = Url.Action(nameof(ResetPassword), "Account", new { userId = user.Id, token }, Request.Scheme)!;
            await SendResetEmailAsync(model.Email, resetLink);
        }

        // Do not disclose whether an account exists for the supplied address.
        _notyf.Success("إذا كان البريد مسجلًا فسيتم إرسال رابط استعادة كلمة المرور.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [AllowAnonymous]
    [NonAction]
    public IActionResult ResetPassword(Guid userId, string token)
    {
        if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
            return BadRequest("رابط الاستعادة غير صالح.");
        return View(new ResetPasswordViewModel { UserId = userId, Token = token });
    }

    [HttpPost]
    [AllowAnonymous]
    [NonAction]
    public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var user = await _userManager.FindByIdAsync(model.UserId.ToString());
        if (user is null)
            return RedirectToAction(nameof(Login));

        var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }
        await _userManager.ResetAccessFailedCountAsync(user);
        _notyf.Success("تم تحديث كلمة المرور.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.MinistryManager + "," + RoleNames.DirectorateManager + "," + RoleNames.Manager + "," + RoleNames.Student + "," + RoleNames.Teacher)]
    public IActionResult NewPassword() => View();

    [HttpPost]
    [Authorize(Roles = RoleNames.Admin + "," + RoleNames.MinistryManager + "," + RoleNames.DirectorateManager + "," + RoleNames.Manager + "," + RoleNames.Student + "," + RoleNames.Teacher)]
    public async Task<IActionResult> NewPassword(NewPasswordViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user is null)
            return Challenge();

        var result = await _userManager.ChangePasswordAsync(user, model.LastPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            AddIdentityErrors(result);
            return View(model);
        }
        await _signInManager.RefreshSignInAsync(user);
        _notyf.Success("تم تحديث كلمة المرور.");
        return RedirectToAction("IndexProfile", "Profile");
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult AccessDenied()
    {
        _notyf.Warning("لا تملك صلاحية الوصول إلى هذه الصفحة.");
        return RedirectToAction("Index", "Home");
    }

    private async Task<bool> LinkProfileAsync(Guid userId, string role, Guid profileId)
    {
        switch (role)
        {
            case RoleNames.MinistryManager:
            case RoleNames.DirectorateManager:
                return false; // Directorate accounts are provisioned administratively, never by public registration.
            case RoleNames.Admin:
            case RoleNames.Manager:
                var manager = await _context.Menegars.SingleOrDefaultAsync(x => x.Id == profileId);
                if (manager is null || manager.ApplicationUserId.HasValue) return false;
                manager.ApplicationUserId = userId;
                return true;
            case RoleNames.Teacher:
                var teacher = await _context.Teachers.SingleOrDefaultAsync(x => x.Id == profileId);
                if (teacher is null || teacher.ApplicationUserId.HasValue) return false;
                teacher.ApplicationUserId = userId;
                return true;
            case RoleNames.Student:
                var student = await _context.Students.SingleOrDefaultAsync(x => x.Id == profileId);
                if (student is null || student.ApplicationUserId.HasValue) return false;
                student.ApplicationUserId = userId;
                return true;
            default:
                return false;
        }
    }

    private async Task<bool> PopulateLegacySessionAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        var role = roles.SingleOrDefault();
        Guid id;
        Guid school;
        string? name;

        switch (role)
        {
            case RoleNames.MinistryManager:
                var ministryManager = await _context.MinistryManagers.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted && x.Ministry.IsActive);
                if (ministryManager is null) return false;
                (id, school, name) = (ministryManager.Id, Guid.Empty, ministryManager.Name);
                HttpContext.Session.SetGuid("Ministry", ministryManager.MinistryId);
                break;
            case RoleNames.DirectorateManager:
                var directorateManager = await _context.DirectorateManagers.AsNoTracking()
                    .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted && x.Directorate.IsActive);
                if (directorateManager is null) return false;
                (id, school, name) = (directorateManager.Id, Guid.Empty, directorateManager.Name);
                HttpContext.Session.SetGuid("Directorate", directorateManager.DirectorateId);
                break;
            case RoleNames.Admin:
            case RoleNames.Manager:
                var manager = await _context.Menegars.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted);
                if (manager is null) return false;
                (id, school, name) = (manager.Id, manager.IdSchool ?? Guid.Empty, manager.Name);
                break;
            case RoleNames.Teacher:
                var teacher = await _context.Teachers.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted);
                if (teacher is null) return false;
                (id, school, name) = (teacher.Id, teacher.IdSchool ?? Guid.Empty, teacher.Name);
                break;
            case RoleNames.Student:
                var student = await _context.Students.AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeletedStudent);
                if (student is null) return false;
                (id, school, name) = (student.Id, student.IdSchool ?? Guid.Empty, student.Name);
                break;
            default:
                return false;
        }

        HttpContext.Session.SetGuid("Id", id);
        HttpContext.Session.SetGuid("School", school);
        HttpContext.Session.SetString("UserName", user.UserName ?? string.Empty);
        HttpContext.Session.SetString("Role", role);
        HttpContext.Session.SetString("Name", name ?? string.Empty);
        return true;
    }

    private void ClearPendingRegistration()
    {
        foreach (var key in new[] { "PendingEmail", "PendingRole", "PendingProfileId", "PendingSchool", "PendingName" })
            HttpContext.Session.Remove(key);
    }

    private void AddIdentityErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
            ModelState.AddModelError(string.Empty, error.Description);
    }

    private static async Task SendResetEmailAsync(string email, string resetLink)
    {
        var host = Environment.GetEnvironmentVariable("MAILTRAP_HOST")
            ?? throw new InvalidOperationException("MAILTRAP_HOST is not configured.");
        var portText = Environment.GetEnvironmentVariable("MAILTRAP_PORT");
        if (!int.TryParse(portText, out var port))
            throw new InvalidOperationException("MAILTRAP_PORT is not configured.");

        using var smtp = new SmtpClient();
        await smtp.ConnectAsync(host, port, false);
        await smtp.AuthenticateAsync(
            Environment.GetEnvironmentVariable("MAILTRAP_USERNAME"),
            Environment.GetEnvironmentVariable("MAILTRAP_PASSWORD"));
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("School System", "no-reply@example.com"));
        message.To.Add(MailboxAddress.Parse(email));
        message.Subject = "Password Reset Request";
        message.Body = new TextPart("plain") { Text = $"Reset your password using this link: {resetLink}" };
        await smtp.SendAsync(message);
        await smtp.DisconnectAsync(true);
    }
}

