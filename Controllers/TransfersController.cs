using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Data;
using SchoolSystem.Filters;

namespace SchoolSystem.Controllers;

[AuthorizeRoles(RoleNames.Admin, RoleNames.DirectorateManager)]
public sealed class TransfersController : Controller
{
    public IActionResult Index() => View();
}
