using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Identity;
using SchoolSystem.Data;

namespace SchoolSystem.Controllers;

// Retains the legacy /AccountApi/* routes while sharing the Identity implementation.
public sealed class AccountApiController : AccountController
{
    public AccountApiController(
        SystemSchoolDbContext context,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        INotyfService notyf,
        IAccountService accountService)
        : base(context, userManager, signInManager, roleManager, notyf, accountService)
    {
    }
}
