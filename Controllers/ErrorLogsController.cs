using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;

public class ErrorLogsController : Controller
{
    private readonly SystemSchoolDbContext _context;

    public ErrorLogsController(SystemSchoolDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var logs = await _context.ErrorLogs
            .OrderByDescending(e => e.Id)
            .ToListAsync();
        return View(logs);
    }
    public IActionResult ThrowError()
    {
        throw new Exception("اختبار تسجيل الأخطاء");
    }
}
