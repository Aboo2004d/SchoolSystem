using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Data;
using SchoolSystem.Filters;

namespace SchoolSystem.Controllers;

[AuthorizeRoles(RoleNames.DirectorateManager)]
public sealed class DirectorateController : Controller
{
    private readonly ISessionValidatorService _sessionValidator;

    public DirectorateController(ISessionValidatorService sessionValidator) => _sessionValidator = sessionValidator;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var access = await _sessionValidator.ValidateDirectorateManagerSessionAsync(HttpContext, "Directorate/Index");
        return access.IsValid ? View() : Forbid();
    }

    [HttpGet]
    public async Task<IActionResult> Schools()
    {
        var access = await _sessionValidator.ValidateDirectorateManagerSessionAsync(HttpContext, "Directorate/Schools");
        return access.IsValid ? View() : Forbid();
    }

    [HttpGet]
    public Task<IActionResult> ActiveSchools() => DirectoryViewAsync("activeSchools", "المدارس الفعالة", "المدارس المفعلة في المديرية ومؤشراتها الأساسية.");

    [HttpGet]
    public Task<IActionResult> Managers() => DirectoryViewAsync("managers", "مديرو المدارس", "بيانات مديري مدارس المديرية وحالة حساباتهم.");

    [HttpGet]
    public Task<IActionResult> Teachers() => DirectoryViewAsync("teachers", "معلمو المديرية", "بيانات المعلمين وتغطيتهم للمواد والصفوف.");

    [HttpGet]
    public Task<IActionResult> Students() => DirectoryViewAsync("students", "طلاب المديرية", "بيانات الطلاب ومدارسهم وصفوفهم وحالة حساباتهم.");

    [HttpGet]
    public Task<IActionResult> Classes() => DirectoryViewAsync("classes", "صفوف المديرية", "توزيع الصفوف على المدارس وأعداد الطلاب والمعلمين.");

    [HttpGet]
    public Task<IActionResult> CreateManager() => PersonFormAsync("manager", "إضافة مدير مدرسة");

    [HttpGet]
    public Task<IActionResult> CreateTeacher() => PersonFormAsync("teacher", "إضافة معلم");

    [HttpGet]
    public Task<IActionResult> CreateStudent() => PersonFormAsync("student", "إضافة طالب");

    private async Task<IActionResult> PersonFormAsync(string type, string title)
    {
        var access = await _sessionValidator.ValidateDirectorateManagerSessionAsync(HttpContext, $"Directorate/Create{type}");
        if (!access.IsValid) return Forbid();
        ViewBag.PersonType = type;
        ViewBag.FormTitle = title;
        return View("CreatePerson");
    }
    private async Task<IActionResult> DirectoryViewAsync(string type, string title, string description)
    {
        var access = await _sessionValidator.ValidateDirectorateManagerSessionAsync(HttpContext, $"Directorate/{type}");
        if (!access.IsValid) return Forbid();
        ViewBag.ReportType = type;
        ViewBag.ReportTitle = title;
        ViewBag.ReportDescription = description;
        ViewBag.CreateAction = type switch
        {
            "managers" => "CreateManager",
            "teachers" => "CreateTeacher",
            "students" => "CreateStudent",
            _ => null
        };
        ViewBag.CreateLabel = type switch
        {
            "managers" => "إضافة مدير مدرسة",
            "teachers" => "إضافة معلم",
            "students" => "إضافة طالب",
            _ => null
        };
        return View("Directory");
    }
    [HttpGet]
    public async Task<IActionResult> SchoolDetails(Guid id)
    {
        var access = await _sessionValidator.ValidateDirectorateSchoolAccessAsync(HttpContext, id, "Directorate/SchoolDetails");
        if (!access.IsValid) return Forbid();
        ViewBag.SchoolId = id;
        return View();
    }

    [HttpGet]
    public async Task<IActionResult> CreateSchool()
    {
        var access = await _sessionValidator.ValidateDirectorateManagerSessionAsync(HttpContext, "Directorate/CreateSchool");
        return access.IsValid ? View() : Forbid();
    }

    [HttpGet]
    public async Task<IActionResult> EditSchool(Guid id)
    {
        var access = await _sessionValidator.ValidateDirectorateSchoolAccessAsync(HttpContext, id, "Directorate/EditSchool");
        if (!access.IsValid) return Forbid();
        ViewBag.SchoolId = id;
        return View();
    }
}
