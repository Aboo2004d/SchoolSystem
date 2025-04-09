using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace SchoolSystem.Filters
{
    public class AuthorizeRolesAttribute : TypeFilterAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles) : base(typeof(AuthorizeRolesFilter))
        {
            Arguments = new object[] { roles };
        }
    }

    public class AuthorizeRolesFilter : IAuthorizationFilter
    {
        private readonly string[] _roles;

        public AuthorizeRolesFilter(string[] roles)
        {
            _roles = roles;
        }

        public void OnAuthorization(AuthorizationFilterContext context)
        {
            // 1. الحصول على دور المستخدم من الـ Session
            var role = context.HttpContext.Session.GetString("Role");

            // 2. إذا لم يكن الدور مسموحًا به أو غير موجود، يمنع الوصول
            if (string.IsNullOrEmpty(role) || !_roles.Contains(role))
            {
                context.Result = new Microsoft.AspNetCore.Mvc.ForbidResult();
            }
        }
    }
}