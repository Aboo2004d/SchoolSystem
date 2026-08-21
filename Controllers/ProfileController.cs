using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Models;

[Authorize(Roles = RoleNames.Admin + "," + RoleNames.MinistryManager + "," + RoleNames.DirectorateManager + "," + RoleNames.Manager + "," + RoleNames.Teacher + "," + RoleNames.Student)]
public class ProfileController : Controller
{
    private readonly SystemSchoolDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly INotyfService _notyf;

    public ProfileController(SystemSchoolDbContext context, UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager, INotyfService notyf)
    {
        _context = context;
        _userManager = userManager;
        _signInManager = signInManager;
        _notyf = notyf;
    }

    public async Task<IActionResult> IndexProfile()
    {
        var data = await LoadCurrentProfileAsync();
        return data is null ? Challenge() : View(data.Value.profile);
    }

    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var data = await LoadCurrentProfileAsync();
        if (id == Guid.Empty) return BadRequest();
        if (data is null || data.Value.profile.Id != id) return Forbid();
        var p = data.Value.profile;
        return View(new EditProfileViewModel
        {
            Id = id, Name = p.Name, Email = p.Email, UserName = p.UserName, Phone = p.Phone,
            Role = data.Value.role, IdNumber = p.IdNumber, TheDate = p.TheDate, City = p.City,
            Area = p.Area, School = p.School, TheClass = p.TheClass
        });
    }

    [HttpPost]
    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        if (!ModelState.IsValid || model.Id == Guid.Empty) return View(model);
        var user = await _userManager.GetUserAsync(User);
        if (user is null || !user.IsActive) return Challenge();
        var role = (await _userManager.GetRolesAsync(user)).SingleOrDefault();
        if (!await UpdateLinkedProfileAsync(user.Id, role, model.Id, model)) return Forbid();

        var oldName = user.UserName;
        var userNameResult = await _userManager.SetUserNameAsync(user, model.UserName);
        if (!userNameResult.Succeeded) { AddErrors(userNameResult); return View(model); }
        var emailResult = await _userManager.SetEmailAsync(user, model.Email);
        if (!emailResult.Succeeded) { AddErrors(emailResult); return View(model); }

        var image = await _context.ProfileImages.SingleOrDefaultAsync(x => x.UserName == oldName);
        if (image is not null) { image.UserName = model.UserName; image.Email = model.Email; }
        await _context.SaveChangesAsync();
        await _signInManager.RefreshSignInAsync(user);
        HttpContext.Session.SetString("UserName", model.UserName);
        HttpContext.Session.SetString("Name", model.Name);
        _notyf.Success("تم تحديث الملف الشخصي.");
        return RedirectToAction(nameof(IndexProfile));
    }

    private async Task<(ProfileViewModel profile, string role)?> LoadCurrentProfileAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user is null || !user.IsActive) return null;
        var role = (await _userManager.GetRolesAsync(user)).SingleOrDefault();
        string? name = null, phone = null, email = null, city = null, area = null, school = null, className = null;
        Guid id = Guid.Empty;
        int idNumber = 0;
        DateOnly date = default;
        if (role == RoleNames.MinistryManager)
        {
            var p = await _context.MinistryManagers.Include(x => x.Ministry).AsNoTracking()
                .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted && x.Ministry.IsActive);
            if (p is null) return null;
            (id, name, phone, email, idNumber, school) =
                (p.Id, p.Name, p.Phone, p.Email, p.IdNumber ?? 0, p.Ministry.Name);
        }
        else if (role == RoleNames.DirectorateManager)
        {
            var p = await _context.DirectorateManagers.Include(x => x.Directorate).AsNoTracking()
                .SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id && !x.IsDeleted);
            if (p is null) return null;
            (id, name, phone, email, city, area, idNumber, school) =
                (p.Id, p.Name, p.Phone, p.Email, p.City, p.Area, p.IdNumber ?? 0, p.Directorate.Name);
        }
        else if (role is RoleNames.Admin or RoleNames.Manager)
        {
            var p = await _context.Menegars.Include(x => x.IdSchoolNavigation).AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            if (p is null) return null;
            (id, name, phone, email, city, area, idNumber, date, school) = (p.Id, p.Name, p.Phone, p.Email, p.City, p.Area, p.IdNumber ?? 0, p.TheDate ?? default, p.IdSchoolNavigation?.Name);
        }
        else if (role == RoleNames.Teacher)
        {
            var p = await _context.Teachers.Include(x => x.IdSchoolNavigation).AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            if (p is null) return null;
            (id, name, phone, email, city, area, idNumber, date, school) = (p.Id, p.Name, p.Phone, p.Email, p.City, p.Area, p.IdNumber ?? 0, p.TheDate ?? default, p.IdSchoolNavigation?.Name);
        }
        else if (role == RoleNames.Student)
        {
            var p = await _context.Students.Include(x => x.IdSchoolNavigation).Include(x => x.IdClassNavigation).AsNoTracking().SingleOrDefaultAsync(x => x.ApplicationUserId == user.Id);
            if (p is null) return null;
            (id, name, phone, email, city, area, idNumber, date, school, className) = (p.Id, p.Name, p.Phone, p.Email, p.City, p.Area, p.IdNumber ?? 0, p.TheDate ?? default, p.IdSchoolNavigation?.Name, p.IdClassNavigation?.Name);
        }
        else return null;

        var image = await _context.ProfileImages.AsNoTracking().SingleOrDefaultAsync(x => x.UserName == user.UserName);
        var relativePath = image?.ProfileImagePath?.TrimStart('/', '\\');
        var exists = relativePath is not null && System.IO.File.Exists(Path.Combine(Directory.GetCurrentDirectory(), relativePath));
        return (new ProfileViewModel
        {
            Id = id, Name = name ?? "", Phone = phone ?? "", Email = email ?? user.Email ?? "",
            UserName = user.UserName ?? "", City = city ?? "", Area = area ?? "", IdNumber = idNumber,
            TheDate = date, School = school ?? "", TheClass = className, Role = role,
            PhotoPath = image?.ProfileImagePath, PhotoExists = exists
        }, role);
    }

    private async Task<bool> UpdateLinkedProfileAsync(Guid userId, string? role, Guid requestedId, EditProfileViewModel model)
    {
        if (role == RoleNames.MinistryManager)
        {
            var p = await _context.MinistryManagers.SingleOrDefaultAsync(x =>
                x.ApplicationUserId == userId && x.Id == requestedId && !x.IsDeleted);
            if (p is null) return false;
            (p.Name, p.Phone, p.Email) = (model.Name, model.Phone, model.Email);
            return true;
        }
        if (role == RoleNames.DirectorateManager)
        {
            var p = await _context.DirectorateManagers.SingleOrDefaultAsync(x =>
                x.ApplicationUserId == userId && x.Id == requestedId && !x.IsDeleted);
            if (p is null) return false;
            (p.Name, p.Phone, p.Email, p.City, p.Area) =
                (model.Name, model.Phone, model.Email, model.City, model.Area);
            return true;
        }
        if (role is RoleNames.Admin or RoleNames.Manager)
        {
            var p = await _context.Menegars.SingleOrDefaultAsync(x => x.ApplicationUserId == userId && x.Id == requestedId);
            if (p is null) return false;
            (p.Name, p.Phone, p.Email, p.City, p.Area, p.TheDate) = (model.Name, model.Phone, model.Email, model.City, model.Area, model.TheDate);
            return true;
        }
        if (role == RoleNames.Teacher)
        {
            var p = await _context.Teachers.SingleOrDefaultAsync(x => x.ApplicationUserId == userId && x.Id == requestedId);
            if (p is null) return false;
            (p.Name, p.Phone, p.Email, p.City, p.Area, p.TheDate) = (model.Name, model.Phone, model.Email, model.City, model.Area, model.TheDate);
            return true;
        }
        if (role == RoleNames.Student)
        {
            var p = await _context.Students.SingleOrDefaultAsync(x => x.ApplicationUserId == userId && x.Id == requestedId);
            if (p is null) return false;
            (p.Name, p.Phone, p.Email, p.City, p.Area, p.TheDate) = (model.Name, model.Phone, model.Email, model.City, model.Area, model.TheDate);
            return true;
        }
        return false;
    }

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors) ModelState.AddModelError(string.Empty, error.Description);
    }
}



