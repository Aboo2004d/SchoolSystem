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
    private readonly EncryptionHelper _encryptionHelper;

    public ProfileController(SystemSchoolDbContext context, INotyfService notyf, IErrorLoggerService logger, EncryptionHelper encryptionHelper)
    {
        _logger = logger;
        _context = context;
        _notyf = notyf;
        _encryptionHelper = encryptionHelper;
    }

    [AuthorizeRoles("admin", "Student", "Teacher")] // ضمان أن المستخدم مصادق عليه للوصول إلى هذه الصفحة
    public async Task<IActionResult> IndexProfile()
    {
        // الحصول على بيانات المستخدم الحالي من الهوية (Claims)
        var username = HttpContext.Session.GetString("UserName");

        if (username == null)
        {
            _notyf.Error("انتهت صلاحية الجلسة");
            return RedirectToAction("Logout", "Account"); // أو إعادة التوجيه إلى تسجيل الدخول
        }

        Acount? user =await _context.Acounts.SingleOrDefaultAsync(u => u.UsersName == username && u.IsActive == true);

        if (user == null || user.IsActive == false)
        {
            _notyf.Error("انتهت صلاحية الجلسة");
            await _logger.LogAsync(new Exception($"حدث خطأ اثناء استرجاع بيانات المستخدم "), "Profile/IndexProfile");
            return RedirectToAction("Logout", "Account");
        }
        
        if (user.Role == "admin")
        {
            return View(nameof(IndexProfile),await  ProfileAdmin(user));
        }
        else if (user.Role == "Student")
        {
            return View(nameof(IndexProfile), await ProfileStudent(user));
        }
        else if (user.Role == "Teacher")
        {
            return View(nameof(IndexProfile),await  ProfileTeacher(user));
        }
        else
        {
            return RedirectToAction("Logout", "Account");
        }
    }


    // GET: Menegar/Edit/5
    
    [AuthorizeRoles("admin", "Student", "Teacher")]
    public async Task<IActionResult> Edit(string id){
       int Id;

        try
        {
            Id = _encryptionHelper.DecryptInt(id);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decoding ID: {ex.Message}");
            _notyf.Error("معرف غير صالح أو تم التلاعب به.");
            return View(nameof(IndexProfile));
        }

        Console.WriteLine($"IdSend: {Id}");
        if (Id <= 0)
        {
            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
            await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "Profile/Edit");
            return View(nameof(IndexProfile));
        }
        Console.WriteLine(12);
        string role = HttpContext.Session.GetString("Role")??"null";
        switch (role)
        {
            case "admin":
                var (menegar, status) =await EditGetAdmin(Id, role);
                if(!status)
                {
                    return View(nameof(IndexProfile));
                }
                return View(menegar);
                
            case "Teacher":
                Console.WriteLine(1234);
                var (teacher, status1) =await EditGetTeacher(Id, role);
                Console.WriteLine(12345678);
                Console.WriteLine($"status1: {status1}");
                Console.WriteLine($"teacher: {teacher.Name}");
                if (!status1)
                {
                    return View(nameof(IndexProfile));
                }
                Console.WriteLine(123456);
                return View(teacher);
            case "Student":
                var (student, status2) =await EditGetStudent(Id, role);
                if(!status2)
                {
                    return View(nameof(IndexProfile));
                }
                return View(student);
                default:
                {
                    _notyf.Error("انتهت صلاحية الجلسة");
                    await _logger.LogAsync(new Exception("انتهت صلاحية الجلسة"), "Profile/Edit");
                    return RedirectToAction("Logout", "Account");
                }
        }
    }

    // POST: Menegar/Edit/5
    [HttpPost]
    [AuthorizeRoles("admin", "Student", "Teacher")]

    public async Task<IActionResult> Edit(EditProfileViewModel model)
    {
        Console.WriteLine($"IdSend1: {model.Id}");
        
        if (model.Id == null)
        {
            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
            await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "Profile/Edit");
            return View(nameof(IndexProfile));
        }

        int realId;

        try
        {
            realId = _encryptionHelper.DecryptInt(model.Id);
            Console.WriteLine($"Decrypted ID: {realId}");

        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error decoding ID: {ex.Message}");
            _notyf.Error("معرف غير صالح أو تم التلاعب به.");
            return View(nameof(IndexProfile));
        }

        if (!ModelState.IsValid)
        {
            /*foreach (var key in ModelState.Keys)
            {
                var value = ModelState[key].AttemptedValue;  // القيمة التي حاول المستخدم إرسالها
                var errors = ModelState[key].Errors;        // أخطاء التحقق إن وجدت

                Console.WriteLine($"Key: {key}, Value: {value}");
                foreach (var error in errors)
                {
                    Console.WriteLine($"Error: {error.ErrorMessage}");
                }
            }
            Console.WriteLine($"IdSend: {model.Id}");
*/
            _notyf.Error("خطأ بالبيانات المرسلة");
            return View(model);
        }
        try
        {
            Console.WriteLine(123);
            string role = HttpContext.Session.GetString("Role") ?? "Null";
            
            if (role == "admin")
            {
                if (await EditAdmin(realId, model))
                    return RedirectToAction("IndexProfile");

            }
            else if (role == "Teacher")
            {
                if (await EditTeacher(realId, model))
                    return RedirectToAction("IndexProfile");
            }
            else if (role == "Student")
            {
                if (await EditStudent(realId, model))
                    return RedirectToAction("IndexProfile");
            }
            else
            {
                _notyf.Error("انتهت صلاحية الجلسة");
                await _logger.LogAsync(new Exception("انتهت صلاحية الجلسة"), "Profile/Edit");
                return RedirectToAction("Logout", "Account");
            }
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(ex, "Profile/Edit");
            _notyf.Error("حدث خطأ اثناء حفظ البيانات\nحاول لاحقا");
        }

        return View(model);
    }

    private async Task<(EditProfileViewModel model, bool status)> EditGetAdmin(int id, string role)
    {

        var menegar = await _context.Menegars.Include(t => t.IdSchoolNavigation).FirstOrDefaultAsync(m => m.Id == id);
        if (menegar == null)
        {
            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
            await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "Profile/Edit");
            return (new EditProfileViewModel(), false);
        }

        var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.IdUser == menegar.Id && ac.Role == role);
        if (user == null)
        {
            _notyf.Error("حدث خطأ اثناء استرجاع بيانات المستخدم");
            await _logger.LogAsync(new Exception($"حدث خطأ اثناء استرجاع بيانات المستخدم بسبب {role }"), "Profile/EditGetAdmin");
            return (new EditProfileViewModel(), false);
        }
        if (user.IsActive == false)
        {
            _notyf.Error("حساب محذوف");
            await _logger.LogAsync(new Exception($"حساب محذوف {user.IsActive }"), "Profile/EditGetAdmin");
            return (new EditProfileViewModel(), false);
        }

        var model = new EditProfileViewModel
        {
            Id = menegar.Id.ToString(),
            UserName = user.UsersName,
            Email = user.Email ?? "Null",
            Name = menegar.Name ?? "Null",
            Phone = menegar.Phone ?? "0",
            Role = user.Role ?? "Null",
            TheDate = menegar.TheDate ?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
            IdNumber = menegar.IdNumber ?? 0,
            City = menegar.City ?? "Null",
            Area = menegar.Area ?? "Null",
            School = menegar.IdSchoolNavigation?.Name ?? "Null",
        };
        return (model, true);
    }
    
    private async Task<(EditProfileViewModel model, bool status)> EditGetTeacher(int id, string role)
    {
        Console.WriteLine($"IdSend: {id}");
        Console.WriteLine($"Role: {role}");
        Teacher? teacher = await _context.Teachers.Where(m => m.Id == id).Include(t => t.IdSchoolNavigation).FirstOrDefaultAsync();
        Console.WriteLine($"Teacher: {teacher.Name}");
        if (teacher == null)
        {
            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
            await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "Profile/Edit");
            return (new EditProfileViewModel(), false);
        }

        var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.IdUser == teacher.Id && ac.Role == role);
        if (user == null)
        {
            _notyf.Error("حدث خطأ اثناء استرجاع بيانات المستخدم");
            await _logger.LogAsync(new Exception($"حدث خطأ اثناء استرجاع بيانات المستخدم بسبب {role }"), "Profile/EditGetAdmin");
            return (new EditProfileViewModel(), false);
        }
        if (user.IsActive == false)
        {
            _notyf.Error("حساب محذوف");
            await _logger.LogAsync(new Exception($"حساب محذوف {user.IsActive }"), "Profile/EditGetAdmin");
            return (new EditProfileViewModel(), false);
        }

        var model = new EditProfileViewModel
        {
            Id = teacher.Id.ToString(),
            UserName = user.UsersName,
            Email = user.Email ?? "Null",
            Name = teacher.Name ?? "Null",
            Phone = teacher.Phone ?? "0",
            Role = user.Role ?? "Null",
            TheDate = teacher.TheDate ?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
            IdNumber = teacher.IdNumber ?? 0,
            City = teacher.City ?? "Null",
            Area = teacher.Area ?? "Null",
            School = teacher.IdSchoolNavigation?.Name ?? "Null",
        };
        return (model, true);
    }
    
    private async Task<(EditProfileViewModel model, bool status)> EditGetStudent(int id, string role)
    {
        Student? student = await _context.Students.Include(t => t.IdSchoolNavigation).Include(s => s.IdClassNavigation).SingleOrDefaultAsync(m => m.Id == id);
        if (student == null)
        {
            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
            await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "Profile/Edit");
            return (new EditProfileViewModel(), false);
        }

        var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.IdUser == student.Id && ac.Role == role);
        if (user == null)
        {
            _notyf.Error("حدث خطأ اثناء استرجاع بيانات المستخدم");
            await _logger.LogAsync(new Exception($"حدث خطأ اثناء استرجاع بيانات المستخدم بسبب {role }"), "Profile/EditGetAdmin");
            return (new EditProfileViewModel(), false);
        }
        if (user.IsActive == false)
        {
            _notyf.Error("حساب محذوف");
            await _logger.LogAsync(new Exception($"حساب محذوف {user.IsActive }"), "Profile/EditGetAdmin");
            return (new EditProfileViewModel(), false);
        }

        var model = new EditProfileViewModel
        {
            Id = student.Id.ToString(),
            UserName = user.UsersName,
            Email = user.Email ?? "Null",
            Name = student.Name ?? "Null",
            Phone = student.Phone ?? "0",
            Role = user.Role ?? "Null",
            TheDate = student.TheDate ?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day),
            IdNumber = student.IdNumber ?? 0,
            City = student.City ?? "Null",
            Area = student.Area ?? "Null",
            School = student.IdSchoolNavigation?.Name ?? "Null",
            TheClass = student.IdClassNavigation?.Name ?? "Null",
        };
        return (model, true);
    }
    
    private async Task<bool> EditAdmin(int id, EditProfileViewModel model)
    {
        var menegar = await _context.Menegars.Include(t => t.IdSchoolNavigation).SingleOrDefaultAsync(m => m.Id == id);
        if (menegar == null)
        {
            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
            await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "Profile/Edit");
            return false;
        }

        var user = await _context.Acounts.SingleOrDefaultAsync(ac => ac.IdUser == menegar.Id && ac.Role == "admin");
         // التحقق من وجود المستخدم
        if (user == null)
        {
            _notyf.Error("حدث خطأ اثناء استرجاع بيانات المستخدم");
            await _logger.LogAsync(new Exception($"حدث خطأ اثناء استرجاع بيانات المستخدم "), "Profile/EditGetAdmin");
            return false;
        }
        if (user.IsActive == false)
        {
            _notyf.Error("حساب محذوف");
            await _logger.LogAsync(new Exception($"حساب محذوف {user.IsActive }"), "Profile/EditGetAdmin");
            return false;
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser =await _context.Acounts
            .SingleOrDefaultAsync(u => u.UsersName == model.UserName && u.Id != user.Id && u.IsActive == true);

        if (existingUser != null)
        {
            _notyf.Error("اسم المستخدم موجود بالفعل");
            return false;
        }

        ProfileImage? updateData =await _context.ProfileImages.SingleOrDefaultAsync(ip => ip.UserName == user.UsersName);

        if (updateData != null)
        {
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
        user.UsersName = model.UserName;
        user.Email = model.Email;

        try
        {
            _context.SaveChanges();
            HttpContext.Session.SetString("UserName", model.UserName);
            _notyf.Success("تم تحديث الملف الشخصي بنجاح");
            return true;
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(ex, "Profile/EditAdmin");
            _notyf.Error("حدث خطأ أثناء تحديث الملف الشخصي\nحاول لاحقا");
            return false;
        }


    }

    private async Task<bool> EditTeacher(int id, EditProfileViewModel model){
        Teacher? teacher = await _context.Teachers.Include(t => t.IdSchoolNavigation).SingleOrDefaultAsync(m => m.Id == id);
        if (teacher == null)
        {
            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
            await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "Profile/Edit");
            return false;
        }

        var user = await _context.Acounts.SingleOrDefaultAsync(ac => ac.IdUser == teacher.Id && ac.Role == "Teacher");
        if (user == null)
        {
            _notyf.Error("حدث خطأ اثناء استرجاع بيانات المستخدم");
            await _logger.LogAsync(new Exception($"حدث خطأ اثناء استرجاع بيانات المستخدم "), "Profile/EditGetAdmin");
            return false;
        }
        if (user.IsActive == false)
        {
            _notyf.Error("حساب محذوف");
            await _logger.LogAsync(new Exception($"حساب محذوف {user.IsActive }"), "Profile/EditGetAdmin");
            return false;
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser = await _context.Acounts
            .SingleOrDefaultAsync(u => u.UsersName == model.UserName && u.Id != user.Id && u.IsActive == true);

        if (existingUser != null)
        {
            _notyf.Error("UserName already exists");
            return false;
        }
        ProfileImage? updateData =await _context.ProfileImages.SingleOrDefaultAsync(ip => ip.UserName == user.UsersName);
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
        user.UsersName = model.UserName;
        user.Email = model.Email;

        try
        {
            _context.SaveChanges();
            HttpContext.Session.SetString("UserName",model.UserName);
            _notyf.Success("تم تحديث الملف الشخصي بنجاح");
            return true; // توجيه إلى صفحة البروفايل
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(ex,"Profile/EditTeacher");
            _notyf.Error("Something went wrong");
            return false;
        }
    }
    
    private async Task<bool> EditStudent(int id, EditProfileViewModel model){
        Student? student = await _context.Students.Include(t => t.IdSchoolNavigation).Include(s => s.IdClassNavigation).SingleOrDefaultAsync(m => m.Id == id);
        if (student == null)
        {
            _notyf.Error("لا يمكن التلاعب بالبيانات المرسلة");
            await _logger.LogAsync(new Exception("لا يمكن التلاعب بالبيانات المرسلة"), "Profile/Edit");
            return false;
        }

        var user = await _context.Acounts.SingleOrDefaultAsync(ac => ac.IdUser == student.Id && ac.Role == "Student");
        if (user == null)
        {
            _notyf.Error("حدث خطأ اثناء استرجاع بيانات المستخدم");
            await _logger.LogAsync(new Exception($"حدث خطأ اثناء استرجاع بيانات المستخدم "), "Profile/EditGetAdmin");
            return false;
        }
        if (user.IsActive == false)
        {
            _notyf.Error("حساب محذوف");
            await _logger.LogAsync(new Exception($"حساب محذوف {user.IsActive }"), "Profile/EditGetAdmin");
            return false;
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser =await _context.Acounts
            .SingleOrDefaultAsync(u => u.UsersName == model.UserName && u.Id != user.Id && u.IsActive == true);

        if (existingUser != null)
        {
            _notyf.Error("اسم المستخدم موجود بالفعل");
            return false;
        }

        ProfileImage? updateData =await _context.ProfileImages.SingleOrDefaultAsync(ip => ip.UserName == user.UsersName);
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
        user.UsersName = model.UserName;
        user.Email = model.Email;

        try
        {
            _context.SaveChanges();
            HttpContext.Session.SetString("UserName",model.UserName);
            _notyf.Success("تم تحديث الملف الشخصي بنجاح");
            return true; 
        }
        catch (Exception ex)
        {
            await _logger.LogAsync(ex,"Profile/EditStudent");
            _notyf.Error("حدث خطأ أثناء تحديث الملف الشخصي\nحاول لاحقا");
            return false;
        }
    }

    private async Task<ProfileViewModel> ProfileAdmin(Acount account)
    {
        ProfileImage? path =await _context.ProfileImages.Where(ip => ip.UserName == account.UsersName).SingleOrDefaultAsync();
        string photoPath = path?.ProfileImagePath?.TrimStart('/')??"Null";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), photoPath ?? "");
        bool photoExists = System.IO.File.Exists(fullPath);
        Menegar? user = await _context.Menegars.Where(t => t.Id == account.IdUser).Include(t => t.IdSchoolNavigation).SingleOrDefaultAsync();
        var model = new ProfileViewModel
        {
            Id = user.Id, 
            UserName = account.UsersName,
            Email = user?.Email??"Null",
            Name = user?.Name??"Null",
            Phone = user?.Phone??"0",
            Role = "مدير",
            TheDate =user?.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
            IdNumber = user?.IdNumber?? 0,
            City = user?.City??"Null",
            Area = user?.Area??"Null",
            School = user?.IdSchoolNavigation?.Name??"Null",
            PhotoExists = photoExists,
            PhotoPath = path?.ProfileImagePath??"Null"
            
        };


        return model;
        
    }

    private async Task<ProfileViewModel> ProfileStudent(Acount account)
    {
        ProfileImage? path =await _context.ProfileImages.Where(ip => ip.UserName == account.UsersName && account.IsActive == true).SingleOrDefaultAsync();
        string photoPath = path?.ProfileImagePath?.TrimStart('/')??"Null";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), photoPath ?? "");
        bool photoExists = System.IO.File.Exists(fullPath);
        Student? user = await _context.Students.Where(t => t.Id == account.IdUser)
        .Include(t => t.IdSchoolNavigation).Include(t => t.IdClassNavigation).SingleOrDefaultAsync();
        var model = new ProfileViewModel
        {
            Id = user.Id, 
            UserName = account.UsersName,
            Email = user?.Email??"Null",
            Name = user?.Name??"Null",
            Phone = user?.Phone??"0",
            Role = "طالب",
            TheDate =user?.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
            IdNumber = user?.IdNumber?? 0,
            City = user?.City??"Null",
            Area = user?.Area??"Null",
            School = user?.IdSchoolNavigation?.Name??"Null",
            TheClass = user?.IdClassNavigation?.Name??"Null",
            PhotoExists = photoExists,
            PhotoPath = path?.ProfileImagePath??"Null"
            
        };
        return model;
        
    }

    private async Task<ProfileViewModel> ProfileTeacher(Acount account)
    {
        ProfileImage? path =await _context.ProfileImages.Where(ip => ip.UserName == account.UsersName && account.IsActive == true).SingleOrDefaultAsync();
        string photoPath = path?.ProfileImagePath?.TrimStart('/')??"Null";
        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), photoPath ?? "");
        bool photoExists = System.IO.File.Exists(fullPath);
        Teacher? user = await _context.Teachers.Where(t => t.Id == account.IdUser).Include(t => t.IdSchoolNavigation).SingleOrDefaultAsync();
        var model = new ProfileViewModel
        {
            Id = user.Id, 
            UserName = account.UsersName,
            Email = user?.Email??"Null",
            Name = user?.Name??"Null",
            Phone = user?.Phone??"0",
            Role = "معلم",
            TheDate =user?.TheDate?? new DateOnly(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day), 
            IdNumber = user?.IdNumber?? 0,
            City = user?.City??"Null",
            Area = user?.Area??"Null",
            School = user?.IdSchoolNavigation?.Name??"Null",
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
