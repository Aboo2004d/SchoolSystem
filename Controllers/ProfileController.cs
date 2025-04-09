using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

public class ProfileController : Controller
{
    private readonly SystemSchoolDbContext _context;

    public ProfileController(SystemSchoolDbContext context)
    {
        _context = context;
    }

    [AuthorizeRoles("admin", "Student", "Teacher")] // ضمان أن المستخدم مصادق عليه للوصول إلى هذه الصفحة
    public IActionResult IndexProfile()
    {
        // الحصول على بيانات المستخدم الحالي من الهوية (Claims)
        var username = HttpContext.Session.GetString("UserName");

        if (username == null)
        {
            return RedirectToAction("Login", "Account"); // أو إعادة التوجيه إلى تسجيل الدخول
        }

        var user = _context.Acounts.FirstOrDefault(u => u.UserName == username);
        if (user == null)
        {
            return RedirectToAction("Index", "Home");
        }

        object? account = null;
        if (user.Role == "admin")
        {
            account = _context.Menegars.FirstOrDefault(u => u.Email == user.Email); // إذا كان الدور Admin
        }
        else if (user.Role == "Student")
        {
            account = _context.Students.FirstOrDefault(u => u.Email == user.Email); // إذا كان الدور Student
        }
        else if (user.Role == "Teacher")
        {
            account = _context.Teachers.FirstOrDefault(u => u.Email == user.Email); // إذا كان الدور Teacher
        }

        if (account == null)
        {
            return RedirectToAction("Index", "Home"); // إعادة التوجيه إلى الصفحة الرئيسية إذا لم يتم العثور على الحساب
        }

        var model = new ProfileViewModel
        {
            Id = (account as dynamic).Id, 
            UserName = user.UserName,
            Email = user.Email,
            Name = (account as dynamic).Name,  // فقط استخدم dynamic إذا كنت واثقًا من الخصائص
            Phone = (account as dynamic).Phone,
            Role = user.Role
        };

        return View("~/Views/Profile/IndexProfile.cshtml", model); // عرض البيانات مع النموذج
    }

    

    // GET: Menegar/Edit/5
    public async Task<IActionResult> EditAdmin(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var menegar = await _context.Menegars.FindAsync(id);
        if (menegar == null)
        {
            return NotFound();
        }

        var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.Email == menegar.Email);
        if (user == null)
        {
            return NotFound();
        }

        var model = new EditProfileViewModel
        {
            Id = menegar.Id,
            UserName = user.UserName,
            Email = menegar.Email,
            Name = menegar.Name,
            Phone = menegar.Phone,
            Role = user.Role
        };

        return View(model);
    }

    // POST: Menegar/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    [AuthorizeRoles("admin")]
    public async Task<IActionResult> EditAdmin(int id, EditProfileViewModel model)
    {
        Console.WriteLine(model.Name);
        Console.WriteLine(model.Email);
        Console.WriteLine(model.Phone);
        Console.WriteLine(model.UserName);
        foreach (var key in ModelState.Keys)
        {
            var errors = ModelState[key].Errors;
            var value = ModelState[key].AttemptedValue;
            foreach(var error in errors){
                Console.WriteLine($"Key: {key},Value: {value}, Error: {error.ErrorMessage}");
            }
        }
        
        if (id != model.Id)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var menegar = await _context.Menegars.FindAsync(id);
        var account = await _context.Acounts.FirstOrDefaultAsync(a => a.Email == menegar.Email);

        if (menegar == null || account == null)
        {
            return NotFound();
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser = await _context.Acounts
            .FirstOrDefaultAsync(u => u.UserName == model.UserName && u.Id != account.Id);

        if (existingUser != null)
        {
            ModelState.AddModelError("UserName", "اسم المستخدم مُستخدم بالفعل.");
            return View(model);
        }

        // تحديث البيانات
        menegar.Name = model.Name;
        menegar.Phone = model.Phone;
        menegar.Email = model.Email;
        account.UserName = model.UserName;
        account.Email = model.Email;

        try
        {
            _context.Entry(menegar).State = EntityState.Modified;
            _context.Entry(account).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(IndexProfile)); // توجيه إلى صفحة البروفايل
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MenegarExists(menegar.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
    }

    private bool MenegarExists(int id)
    {
        return _context.Menegars.Any(e => e.Id == id);
    }
}
