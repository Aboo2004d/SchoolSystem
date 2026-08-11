using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolSystem.Data;

namespace SchoolSystem.Filters;

public sealed class AuthorizeRolesAttribute : TypeFilterAttribute
{
    public AuthorizeRolesAttribute(params string[] roles) : base(typeof(AuthorizeRolesFilter)) =>
        Arguments = new object[] { roles };
}

public sealed class AuthorizeRolesFilter : IAuthorizationFilter
{
    private readonly string[] _roles;
    public AuthorizeRolesFilter(string[] roles) => _roles = roles;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var user = context.HttpContext.User;
        var allowed = user.Identity?.IsAuthenticated == true && _roles
            .Select(RoleNames.Normalize)
            .Where(role => role is not null)
            .Any(role => user.IsInRole(role!));
        if (!allowed)
            context.Result = new ForbidResult();
    }
}
