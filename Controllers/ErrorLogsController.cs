using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

public class ErrorLogsController : Controller
{
    private readonly SystemSchoolDbContext _context;

    public ErrorLogsController(SystemSchoolDbContext context)
    {
        _context = context;
    }

    [AuthorizeRoles("admin")]
    public async Task<IActionResult> Index(int page = 1, int pageSize = 20)
    {
        var totalLogs = await _context.ErrorLogs.CountAsync();

        var logs = await _context.ErrorLogs
            .OrderByDescending(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var viewModel = new ErrorLogsViewModel
        {
            Logs = logs,
            CurrentPage = page,
            PageSize = pageSize,
            TotalCount = totalLogs
        };

        return View(viewModel);
    }

    public IActionResult ThrowError()
    {
        throw new Exception("اختبار تسجيل الأخطاء");
    }
}
