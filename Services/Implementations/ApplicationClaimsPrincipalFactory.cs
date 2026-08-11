using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using SchoolSystem.Data;

public sealed class ApplicationClaimsPrincipalFactory : UserClaimsPrincipalFactory<ApplicationUser, IdentityRole<Guid>>
{
    public ApplicationClaimsPrincipalFactory(UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager, IOptions<IdentityOptions> options)
        : base(userManager, roleManager, options) { }

    protected override async Task<ClaimsIdentity> GenerateClaimsAsync(ApplicationUser user)
    {
        var identity = await base.GenerateClaimsAsync(user);
        identity.AddClaim(new Claim("active", user.IsActive ? "true" : "false"));
        return identity;
    }
}
