using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;

namespace SchoolSystem.Filters;

/// <summary>Defense-in-depth against changing GUID route/query values to another user's resources.</summary>
public sealed class OwnershipAuthorizationFilter : IAsyncActionFilter
{
    private readonly SystemSchoolDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public OwnershipAuthorizationFilter(SystemSchoolDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.HttpContext.User.Identity?.IsAuthenticated != true ||
            context.HttpContext.User.IsInRole(RoleNames.Admin))
        {
            await next();
            return;
        }

        var user = await _users.GetUserAsync(context.HttpContext.User);
        if (user is null || !user.IsActive)
        {
            context.Result = new ForbidResult();
            return;
        }

        var ids = context.ActionArguments
            // Nullable<Guid> with a value is boxed as Guid; null values are intentionally ignored.
            .Where(x => x.Value is Guid)
            .Select(x => (Name: x.Key.ToLowerInvariant(), Id: (Guid)x.Value!))
            .Where(x => x.Id != Guid.Empty)
            .ToArray();
        var controller = context.Controller.GetType().Name.Replace("ApiController", "Controller");

        var allowed = context.HttpContext.User.IsInRole(RoleNames.Teacher)
            ? await IsTeacherRequestAllowedAsync(user.Id, controller, ids)
            : context.HttpContext.User.IsInRole(RoleNames.Student)
                ? await IsStudentRequestAllowedAsync(user.Id, controller, ids)
                : context.HttpContext.User.IsInRole(RoleNames.Manager)
                    ? await IsManagerRequestAllowedAsync(user.Id, controller, ids)
                    : false;

        if (!allowed)
        {
            context.Result = new ForbidResult();
            return;
        }

        await next();
    }

    private async Task<bool> IsTeacherRequestAllowedAsync(Guid userId, string controller, (string Name, Guid Id)[] ids)
    {
        var profile = await _db.Teachers.AsNoTracking()
            .Where(x => x.ApplicationUserId == userId && !x.IsDeleted)
            .Select(x => new { x.Id, x.IdSchool })
            .SingleOrDefaultAsync();
        if (profile is null || !profile.IdSchool.HasValue) return false;

        foreach (var item in ids)
        {
            if (item.Name.Contains("teacher") && item.Id != profile.Id) return false;
            if (item.Name.Contains("student") && !await _db.StudentLectuerTeachers
                    .AnyAsync(x => x.IdTeacher == profile.Id && x.IdStudent == item.Id && x.IdSchool == profile.IdSchool)) return false;
            if (item.Name == "id" && controller.Contains("Grades") &&
                !await _db.Grades.AnyAsync(x => x.GradesId == item.Id && x.IdTeacher == profile.Id)) return false;
            if (item.Name == "id" && controller.Contains("Attendance") &&
                !await _db.Attendances.AnyAsync(x => x.Id == item.Id && x.IdTeacher == profile.Id)) return false;
        }
        return true;
    }

    private async Task<bool> IsStudentRequestAllowedAsync(Guid userId, string controller, (string Name, Guid Id)[] ids)
    {
        var profile = await _db.Students.AsNoTracking()
            .Where(x => x.ApplicationUserId == userId && !x.IsDeletedStudent)
            .Select(x => new { x.Id, x.IdSchool })
            .SingleOrDefaultAsync();
        if (profile is null || !profile.IdSchool.HasValue) return false;

        foreach (var item in ids)
        {
            if (item.Name.Contains("student") && item.Id != profile.Id) return false;
            if (item.Name == "id" && controller.Contains("Grades") &&
                !await _db.Grades.AnyAsync(x => x.GradesId == item.Id && x.IdStudent == profile.Id)) return false;
            if (item.Name == "id" && controller.Contains("Attendance") &&
                !await _db.Attendances.AnyAsync(x => x.Id == item.Id && x.IdStudent == profile.Id)) return false;
        }
        return true;
    }

    private async Task<bool> IsManagerRequestAllowedAsync(Guid userId, string controller, (string Name, Guid Id)[] ids)
    {
        var school = await _db.Menegars.AsNoTracking()
            .Where(x => x.ApplicationUserId == userId && !x.IsDeleted)
            .Select(x => x.IdSchool)
            .SingleOrDefaultAsync();
        if (!school.HasValue) return false;

        foreach (var item in ids)
        {
            if (item.Name.Contains("school") && item.Id != school.Value) return false;
            if (item.Name.Contains("teacher") && !await _db.Teachers.AnyAsync(x => x.Id == item.Id && x.IdSchool == school)) return false;
            if (item.Name.Contains("student") && !await _db.Students.AnyAsync(x => x.Id == item.Id && x.IdSchool == school)) return false;
            if (item.Name.Contains("class") && !await _db.TheClasses.AnyAsync(x => x.Id == item.Id && x.IdSchool == school)) return false;
            if (item.Name.Contains("lectuer") && !await _db.Lectuers.AnyAsync(x => x.Id == item.Id && x.IdSchool == school)) return false;
        }
        return true;
    }
}
