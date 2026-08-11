using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Filters;
using SchoolSystem.Models;
using SchoolSystem.Data;

namespace SchoolSystem.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [AuthorizeRoles(RoleNames.Admin, RoleNames.Manager)]
    public IActionResult AdminDashboard()
    {
        return View();
    }
    
    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }
    
    [AllowAnonymous]
    public IActionResult About()
    {
        return View();
    }
    [AllowAnonymous]
    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
