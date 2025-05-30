using System.Security.Claims;
using AspNetCoreHero.ToastNotification.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using SchoolSystem.Filters;
using SchoolSystem.Models;

public class ProfileController : Controller
{
    private readonly SystemSchoolDbContext _context;

    private readonly INotyfService _notyf;
    private readonly IErrorLoggerService _logger;
    

    public ProfileController(SystemSchoolDbContext context, INotyfService notyf,IErrorLoggerService logger)
    {
        _logger = logger;
        _context = context;
        _notyf = notyf;
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

        var user = _context.Acounts.FirstOrDefault(u => u.UsersName == username);
        if (user == null)
        {
            return RedirectToAction("Index", "Home");
        }
        if (user.Role == "admin")
        {
            var account = _context.Menegars.Include(m => m.IdSchoolNavigation).FirstOrDefault(u => u.Id == user.IdUser); // إذا كان الدور Admin
            if (account == null)
            {
                return NotFound();
            }
            return View(nameof(IndexProfile),ProfileAdmin(user,account));
        }
        else if (user.Role == "Student")
        {
            var account = _context.Students.Include(s => s.IdSchoolNavigation)
            .Include(s => s.IdClassNavigation).FirstOrDefault(u => u.Id == user.IdUser); // إذا كان الدور Student
            if (account == null)
            {
                return NotFound();
            }
            return View(nameof(IndexProfile),ProfileStudent(user,account));
        }
        else if (user.Role == "Teacher")
        {
            var account = _context.Teachers.Include(t => t.IdSchoolNavigation).FirstOrDefault(u => u.Id == user.IdUser); // إذا كان الدور Teacher
            if (account == null)
            {
                return NotFound();
            }
            return View(nameof(IndexProfile),ProfileTeacher(user,account));
        }else{
            return RedirectToAction("Login", "Account");
        }
    }


    // GET: Menegar/Edit/5
    
    [AuthorizeRoles("admin", "Student", "Teacher")]
    public async Task<IActionResult> Edit(int? id){
        if (id == null)
        {
        return NotFound();
        }
        String role = HttpContext.Session.GetString("Role")??"null";
        if (role == "admin")
        {
            var menegar = await _context.Menegars.Include(t => t.IdSchoolNavigation).FirstOrDefaultAsync(m=>m.Id == id);
            if (menegar == null)
            {
                return NotFound();
            }

            var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.IdUser == menegar.Id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Id = menegar.Id, 
                UserName = user.UsersName,
                Email = user.Email??"Null",
                Name = menegar.Name??"Null",
                Phone = menegar.Phone??"0",
                Role = user.Role??"Null",
                TheDate =menegar.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
                IdNumber = menegar.IdNumber?? 0,
                City = menegar.City??"Null",
                Area = menegar.Area??"Null",
                School = menegar.IdSchoolNavigation?.Name??"Null",
            };
            return View(model);
        }
        else if(role == "Teacher"){
            var teacher = await _context.Teachers.Include(t => t.IdSchoolNavigation).FirstOrDefaultAsync(t=>t.Id == id);
            if (teacher == null)
            {
                return NotFound();
            }

            var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.IdUser == teacher.Id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Id = teacher.Id, 
                UserName = user.UsersName,
                Email = user.Email??"Null",
                Name = teacher.Name??"Null",
                Phone = teacher.Phone??"0",
                Role = user.Role??"Null",
                TheDate =teacher.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
                IdNumber = teacher.IdNumber?? 0,
                City = teacher.City??"Null",
                Area = teacher.Area??"Null",
                School = teacher.IdSchoolNavigation?.Name??"Null",
            };
            return View(model);
        }
        else if(role == "Student"){
            var student = await _context.Students.Include(t => t.IdSchoolNavigation).Include(t => t.IdClassNavigation).FirstOrDefaultAsync(s=>s.Id == id);
            if (student == null)
            {
                return NotFound();
            }

            var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.IdUser == student.Id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Id = student.Id, 
                UserName = user.UsersName,
                Email = user.Email??"Null",
                Name = student.Name??"Null",
                Phone = student.Phone??"0",
                Role = user.Role??"Null",
                TheDate =student.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
                IdNumber = student.IdNumber?? 0,
                City = student.City??"Null",
                Area = student.Area??"Null",
                School = student.IdSchoolNavigation?.Name??"Null",
                TheClass = student.IdClassNavigation?.Name??"Null"
            };
            return View(model);
        }
        return NotFound(); // في حالة عدم وجود الدور المناسب
    }

    // POST: Menegar/Edit/5
    [HttpPost]
    [AuthorizeRoles("admin", "Student", "Teacher")]

    public async Task<IActionResult> Edit(int id, EditProfileViewModel model)
    {
        if (id != model.Id)
        {
            return NotFound();
        }
        if (!ModelState.IsValid)
        {
            return View(model);
        }
        try
        {
            string role = HttpContext.Session.GetString("Role") ?? "Null";
            Console.WriteLine($"Role: {role}");
            if (role == "admin")
            {
                if (EditAdmin(id, model))
                    return RedirectToAction("IndexProfile");

            }
            else if (role == "Teacher")
            {
                if (EditTeacher(id, model))
                    return RedirectToAction("IndexProfile");
            }
            else if (role == "Student")
            {
                if (EditStudent(id, model))
                    return RedirectToAction("IndexProfile");
            }
            else
            {
                _notyf.Error("The user not found");
            }
            return View(model);
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(ex, "Profile/EditTeacher");
            _notyf.Error("Something went wrong");
            return View(model);
        }

    }

    
    private bool EditAdmin(int id, EditProfileViewModel model)
    {
        var menegar =  _context.Menegars.FirstOrDefault(m => m.Id == id);
        if (menegar == null)
        {
            _notyf.Error("Menegar not found");
            return false;
        }
        var account =  _context.Acounts.FirstOrDefault(a => a.Email == menegar.Email);

        if (account == null)
        {
            _notyf.Error("Account not found");
            return false;
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser = _context.Acounts
            .FirstOrDefault(u => u.UsersName == model.UserName && u.Id != account.Id);

        if (existingUser != null)
        {
            _notyf.Error("UserName already exists");
            return false;
        }
        ProfileImage updateData = _context.ProfileImages.FirstOrDefault(ip => ip.UserName == account.UsersName);
        if(updateData != null){
            updateData.Email = model.Email;
            updateData.UserName = model.UserName;
        }
        // تحديث البيانات
        menegar.Name = model.Name;
        menegar.Phone = model.Phone;
        menegar.Email = model.Email;
        menegar.City = model.City;
        menegar.Area = model.Area;
        menegar.TheDate = model.TheDate;
        account.UsersName = model.UserName;
        account.Email = model.Email;

        try
        {
            _context.SaveChanges();
            HttpContext.Session.SetString("UserName",model.UserName);
            _notyf.Success("Information updated successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogAsync(ex,"Profile/EditAdmin");
            _notyf.Error("Something went wrong");
            return false;
        }
        
        
    }

    private bool EditTeacher(int id, EditProfileViewModel model){
        var teacher =  _context.Teachers.Where(t => t.Id == id)
            .FirstOrDefault();
        var account = _context.Acounts.FirstOrDefault(a => a.Email == teacher.Email);

        if (teacher == null || account == null)
        {
            return false;
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser = _context.Acounts
            .FirstOrDefault(u => u.UsersName == model.UserName && u.Id != account.Id);

        if (existingUser != null)
        {
            _notyf.Error("UserName already exists");
            return false;
        }
        ProfileImage updateData = _context.ProfileImages.FirstOrDefault(ip => ip.UserName == account.UsersName);
        if(updateData != null){
            updateData.Email = model.Email;
            updateData.UserName = model.UserName;
        }

        // تحديث البيانات
        teacher.Name = model.Name;
        teacher.Phone = model.Phone;
        teacher.Email = model.Email;
        teacher.City = model.City;
        teacher.Area = model.Area;
        teacher.TheDate = model.TheDate;
        account.UsersName = model.UserName;
        account.Email = model.Email;

        try
        {
            _context.SaveChanges();
            HttpContext.Session.SetString("UserName",model.UserName);
            _notyf.Success("تم تحديث الملف الشخصي بنجاح");
            return true; // توجيه إلى صفحة البروفايل
        }
        catch (Exception ex)
        {
            _logger.LogAsync(ex,"Profile/EditTeacher");
            _notyf.Error("Something went wrong");
            return false;
        }
    }
    
    private bool EditStudent(int id, EditProfileViewModel model){
        var student = _context.Students.FirstOrDefault(s => s.Id == id);
        if (student == null)
        {
            _notyf.Error("Student not found");
            return false;
        }
        var account = _context.Acounts.FirstOrDefault(a => a.IdUser == student.Id);
        if ( account == null)
        {
            _notyf.Error("Account not found");
            return false;
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser = _context.Acounts
            .FirstOrDefault(u => u.UsersName == model.UserName && u.Id != account.Id && u.IsActive == false);

        if (existingUser != null)
        {
            _notyf.Error("UserName already exists");
            return false;
        }

        ProfileImage updateData = _context.ProfileImages.FirstOrDefault(ip => ip.UserName == account.UsersName);
        if(updateData != null){
            updateData.Email = model.Email;
            updateData.UserName = model.UserName;
        }

        // تحديث البيانات
        student.Name = model.Name;
        student.Phone = model.Phone;
        student.Email = model.Email;
        student.City = model.City;
        student.Area = model.Area;
        student.TheDate = model.TheDate;
        account.UsersName = model.UserName;
        account.Email = model.Email;

        try
        {
            _context.SaveChanges();
            HttpContext.Session.SetString("UserName",model.UserName);
            _notyf.Success("تم تحديث الملف الشخصي بنجاح");
            return true; // توجيه إلى صفحة البروفايل
        }
        catch (Exception ex)
        {
            _logger.LogAsync(ex,"Profile/EditStudent");
            _notyf.Error("Something went wrong");
            return false;
        }
    }

    private ProfileViewModel ProfileAdmin(Acount user, Menegar account)
    {
        Console.WriteLine($"Id School: {account.IdSchool}");
        Console.WriteLine($"School Name: {account.IdSchoolNavigation?.Name??"Null"}");
        ProfileImage path = _context.ProfileImages.Where(ip => ip.UserName == user.UsersName && ip.Email == user.Email).FirstOrDefault();
        string photoPath = path?.ProfileImagePath?.TrimStart('/')??"Null";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), photoPath ?? "");
        bool photoExists = System.IO.File.Exists(fullPath);
        Console.WriteLine($"Photo: {path}");
        var model = new ProfileViewModel
        {
            Id = account.Id, 
            UserName = user.UsersName,
            Email = user.Email??"Null",
            Name = account.Name??"Null",
            Phone = account.Phone??"0",
            Role = user.Role??"Null",
            TheDate =account.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
            IdNumber = account.IdNumber?? 0,
            City = account.City??"Null",
            Area = account.Area??"Null",
            School = account.IdSchoolNavigation?.Name??"Null",
            PhotoExists = photoExists,
            PhotoPath = path?.ProfileImagePath??"Null"
            
        };


        return model;
        
    }

    private ProfileViewModel ProfileStudent(Acount account, Student user )
    {
        ProfileImage path = _context.ProfileImages.Where(ip => ip.UserName == account.UsersName && account.IsActive == true).FirstOrDefault();
        string photoPath = path?.ProfileImagePath?.TrimStart('/')??"Null";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), photoPath ?? "");
        bool photoExists = System.IO.File.Exists(fullPath);
        Console.WriteLine($"Photo: {path}");
        var model = new ProfileViewModel
        {
            Id = account.Id, 
            UserName = account.UsersName,
            Email = user.Email??"Null",
            Name = user.Name??"Null",
            Phone = user.Phone??"0",
            Role = account.Role??"Null",
            TheDate =user.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
            IdNumber = user.IdNumber?? 0,
            City = user.City??"Null",
            Area = user.Area??"Null",
            School = user.IdSchoolNavigation?.Name??"Null",
            TheClass = user.IdClassNavigation?.Name??"Null",
            PhotoExists = photoExists,
            PhotoPath = path?.ProfileImagePath??"Null"
            
        };
        return model;
        
    }

    private ProfileViewModel ProfileTeacher(Acount account, Teacher user)
        {
            ProfileImage path = _context.ProfileImages.Where(ip => ip.UserName == account.UsersName && account.IsActive == true).FirstOrDefault();
        string photoPath = path?.ProfileImagePath?.TrimStart('/')??"Null";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), photoPath ?? "");
        bool photoExists = System.IO.File.Exists(fullPath);
        Console.WriteLine($"Photo: {path}");
        var model = new ProfileViewModel
        {
            Id = account.Id, 
            UserName = account.UsersName,
            Email = user.Email??"Null",
            Name = user.Name??"Null",
            Phone = user.Phone??"0",
            Role = account.Role??"Null",
            TheDate =user.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
            IdNumber = user.IdNumber?? 0,
            City = user.City??"Null",
            Area = user.Area??"Null",
            School = user.IdSchoolNavigation?.Name??"Null",
            PhotoExists = photoExists,
            PhotoPath = path?.ProfileImagePath??"Null"
            
        };
        return model;
        }

    private bool MenegarExists(int id)
    {
        return _context.Menegars.Any(e => e.Id == id);
    }
}
