using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolSystem.Data;

namespace SchoolSystem.Filters;

public sealed class AuthorizeMinistryAttribute : TypeFilterAttribute
{
    public AuthorizeMinistryAttribute() : base(typeof(AuthorizeMinistryFilter)) { }
}

public sealed class AuthorizeMinistryFilter : IAsyncAuthorizationFilter
{
    private readonly UserManager<ApplicationUser> _users;
    public AuthorizeMinistryFilter(UserManager<ApplicationUser> users) => _users = users;

    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var principal = context.HttpContext.User;
        if (principal.Identity?.IsAuthenticated != true)
        {
            context.Result = new ForbidResult();
            return;
        }

        var user = await _users.GetUserAsync(principal);
        if (user is null && !string.IsNullOrWhiteSpace(principal.Identity?.Name))
            user = await _users.FindByNameAsync(principal.Identity.Name);
        if (user is null)
        {
            var sessionUserName = context.HttpContext.Session.GetString("UserName");
            if (!string.IsNullOrWhiteSpace(sessionUserName)) user = await _users.FindByNameAsync(sessionUserName);
        }
        if (user is null || !user.IsActive ||
            (!await _users.IsInRoleAsync(user, RoleNames.Admin) &&
             !await _users.IsInRoleAsync(user, RoleNames.MinistryManager)))
            context.Result = new ForbidResult();
    }
}
