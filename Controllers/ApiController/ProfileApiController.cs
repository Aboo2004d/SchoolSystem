using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Identity;
using SchoolSystem.Data;

public sealed class ProfileApiController : ProfileController
{
    public ProfileApiController(SystemSchoolDbContext context, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, INotyfService notyf, EncryptionHelper encryptionHelper)
        : base(context, userManager, signInManager, notyf, encryptionHelper) { }
}
