using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Data;
using SchoolSystem.Filters;

namespace SchoolSystem.Controllers;

[AuthorizeMinistry]
public sealed class MinistryController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public IActionResult Directorates() => View();

    [HttpGet] public IActionResult CreateDirectorate() => CreateRecord("directorate", "إضافة مديرية");
    [HttpGet] public IActionResult CreateSchool() => CreateRecord("school", "إضافة مدرسة");
    [HttpGet] public IActionResult CreateDirectorateManager() => CreateRecord("directorateManager", "إضافة مسؤول مديرية");
    [HttpGet] public IActionResult CreateSchoolManager() => CreateRecord("schoolManager", "إضافة مدير مدرسة");
    [HttpGet] public IActionResult CreateTeacher() => CreateRecord("teacher", "إضافة معلم");
    [HttpGet] public IActionResult CreateStudent() => CreateRecord("student", "إضافة طالب");
    private IActionResult CreateRecord(string type, string title) { ViewBag.RecordType = type; ViewBag.FormTitle = title; return View("CreateRecord"); }
    [HttpGet] public IActionResult Schools() => Directory("schools", "مدارس الوزارة", "جميع المدارس التابعة لمديريات الوزارة.", "CreateSchool", "إضافة مدرسة");
    [HttpGet] public IActionResult ActiveSchools() => Directory("activeSchools", "المدارس الفعالة", "المدارس الفعالة التابعة للوزارة.", "CreateSchool", "إضافة مدرسة");
    [HttpGet] public IActionResult DirectorateManagers() => Directory("directorateManagers", "مسؤولو المديريات", "مسؤولو المديريات التابعة للوزارة.", "CreateDirectorateManager", "إضافة مسؤول مديرية");
    [HttpGet] public IActionResult SchoolManagers() => Directory("schoolManagers", "مديرو المدارس", "مديرو المدارس التابعة للوزارة.", "CreateSchoolManager", "إضافة مدير مدرسة");
    [HttpGet] public IActionResult Teachers() => Directory("teachers", "معلمو الوزارة", "المعلمون العاملون في مدارس الوزارة.", "CreateTeacher", "إضافة معلم");
    [HttpGet] public IActionResult Students() => Directory("students", "طلاب الوزارة", "الطلاب المسجلون في مدارس الوزارة.", "CreateStudent", "إضافة طالب");

    private IActionResult Directory(string type, string title, string description, string createAction, string createLabel)
    {
        ViewBag.ReportType = type; ViewBag.ReportTitle = title; ViewBag.ReportDescription = description;
        ViewBag.CreateAction = createAction; ViewBag.CreateLabel = createLabel;
        return View("Directory");
    }

    [HttpGet]
    public IActionResult DirectorateDetails(Guid id)
    {
        ViewBag.DirectorateId = id;
        return View();
    }
}
