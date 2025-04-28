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

        var user = _context.Acounts.FirstOrDefault(u => u.UsersName == username);
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
            UserName = user.UsersName,
            Email = user.Email,
            Name = (account as dynamic).Name,  // فقط استخدم dynamic إذا كنت واثقًا من الخصائص
            Phone = (account as dynamic).Phone,
            Role = user.Role
        };

        return View("~/Views/Profile/IndexProfile.cshtml", model); // عرض البيانات مع النموذج
    }


    // GET: Menegar/Edit/5
    [AuthorizeRoles("admin", "Student", "Teacher")]
    public async Task<IActionResult> Edit(int? id){
        if (id == null)
        {
        return NotFound();
        }
        String role = HttpContext.Session.GetString("Role");
        if (role == "admin"){
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
                UserName = user.UsersName,
                Email = menegar.Email,
                Name = menegar.Name,
                Phone = menegar.Phone,
                Role = user.Role
            };
            return View(model);
        }else if(role == "Teacher"){
            var teacher = await _context.Teachers.FindAsync(id);
            if (teacher == null)
            {
                return NotFound();
            }

            var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.Email == teacher.Email);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Id = teacher.Id,
                UserName = user.UsersName,
                Email = teacher.Email,
                Name = teacher.Name,
                Phone = teacher.Phone,
                Role = user.Role
            };
            return View(model);
        }else if(role == "Student"){
            var student = await _context.Students.FindAsync(id);
            if (student == null)
            {
                return NotFound();
            }

            var user = await _context.Acounts.FirstOrDefaultAsync(ac => ac.Email == student.Email);
            if (user == null)
            {
                return NotFound();
            }

            var model = new EditProfileViewModel
            {
                Id = student.Id,
                UserName = user.UsersName,
                Email = student.Email,
                Name = student.Name,
                Phone = student.Phone,
                Role = user.Role
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
        try{
            String role = HttpContext.Session.GetString("Role");
            if (role == "admin")
            {
                await  EditAdmin(id, model);
                
            }else if(role == "Teacher"){
                await EditTeacher(id, model);
            }else if(role == "Student"){
                await EditStudent(id, model);
            }
            return RedirectToAction(nameof(IndexProfile)); // توجيه إلى صفحة البروفايل
        }catch (DbUpdateConcurrencyException)
        {
            return View(model);
        }
        
    }
    private async Task<IActionResult> EditAdmin(int id, EditProfileViewModel model)
    {
        var menegar = await _context.Menegars.FindAsync(id);
        var account = await _context.Acounts.FirstOrDefaultAsync(a => a.Email == menegar.Email);

        if (menegar == null || account == null)
        {
            return NotFound();
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser = await _context.Acounts
            .FirstOrDefaultAsync(u => u.UsersName == model.UserName && u.Id != account.Id);

        if (existingUser != null)
        {
            ModelState.AddModelError("UserName", "اسم المستخدم مُستخدم بالفعل.");
            return  View(model);
        }

        // تحديث البيانات
        menegar.Name = model.Name;
        menegar.Phone = model.Phone;
        menegar.Email = model.Email;
        account.UsersName = model.UserName;
        account.Email = model.Email;

        try
        {
            _context.Entry(menegar).State = EntityState.Modified;
            _context.Entry(account).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(IndexProfile)); // توجيه إلى صفحة البروفايل
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Console.WriteLine(1);
            Console.WriteLine("Error in EditAdmin method: " + ex.Message);
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

    private async Task<IActionResult> EditTeacher(int id, EditProfileViewModel model){
        var teacher = await _context.Teachers.FindAsync(id);
        var account = await _context.Acounts.FirstOrDefaultAsync(a => a.Email == teacher.Email);

        if (teacher == null || account == null)
        {
            return NotFound();
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser = await _context.Acounts
            .FirstOrDefaultAsync(u => u.UsersName == model.UserName && u.Id != account.Id);

        if (existingUser != null)
        {
            ModelState.AddModelError("UserName", "اسم المستخدم مُستخدم بالفعل.");
            return View(model);
        }

        // تحديث البيانات
        teacher.Name = model.Name;
        teacher.Phone = model.Phone;
        teacher.Email = model.Email;
        account.UsersName = model.UserName;
        account.Email = model.Email;

        try
        {
            _context.Entry(teacher).State = EntityState.Modified;
            _context.Entry(account).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(IndexProfile)); // توجيه إلى صفحة البروفايل
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MenegarExists(teacher.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }
    }
    
    private async Task<IActionResult> EditStudent(int id, EditProfileViewModel model){
        var student = await _context.Students.FindAsync(id);
        var account = await _context.Acounts.FirstOrDefaultAsync(a => a.Email == student.Email);

        if (student == null || account == null)
        {
            return NotFound();
        }

        // التحقق من تكرار اسم المستخدم
        var existingUser = await _context.Acounts
            .FirstOrDefaultAsync(u => u.UsersName == model.UserName && u.Id != account.Id);

        if (existingUser != null)
        {
            ModelState.AddModelError("UserName", "اسم المستخدم مُستخدم بالفعل.");
            return View(model);
        }

        // تحديث البيانات
        student.Name = model.Name;
        student.Phone = model.Phone;
        student.Email = model.Email;
        account.UsersName = model.UserName;
        account.Email = model.Email;

        try
        {
            _context.Entry(student).State = EntityState.Modified;
            _context.Entry(account).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(IndexProfile)); // توجيه إلى صفحة البروفايل
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MenegarExists(student.Id))
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
