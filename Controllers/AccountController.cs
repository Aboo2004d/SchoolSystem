using Microsoft.AspNetCore.Mvc;
using SchoolSystem.Models;
using SchoolSystem.Data;
using SchoolSystem;
using System.Security.Cryptography;
using System.Text;
using System.Net.Mail;
using MimeKit;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

public class AccountController : Controller
{
private readonly SystemSchoolDbContext _context;

public AccountController(SystemSchoolDbContext context)
{
    _context = context;
}


    private bool chick_user()
    {
        Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
        return User.Identity?.IsAuthenticated == true;
    }

    private string HashPassword(string password)
    {
        using (var sha512 = SHA512.Create())
        {
            byte[] bytes = Encoding.UTF8.GetBytes(password); // تحويل النص إلى بايتات
            byte[] hash = sha512.ComputeHash(bytes);         // تشفير البايتات باستخدام SHA512
            return Convert.ToBase64String(hash);             // تحويل النتيجة إلى Base64
        }
    }

    // GET: /Account/Login
    public IActionResult Login()
    {
        Console.WriteLine(chick_user());
        if (chick_user())
        {
            // إذا كان المستخدم مصادقًا عليه، قم بإعادة توجيهه إلى الصفحة الرئيسية
            return RedirectToAction("Index", "Home");
        }
        return View();
    }

    // POST: /Account/Login
    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        
        Console.WriteLine($"username: {model.UserNameOrEmail}");
        Console.WriteLine($"password: {model.Password}");
        if (ModelState.IsValid)
        {
            Console.WriteLine($"username1: {model.UserNameOrEmail}");
            var account = _context.Acounts
                .FirstOrDefault(a => a.UsersName == model.UserNameOrEmail || a.Email == model.UserNameOrEmail);
            Console.WriteLine($"Account: {account != null}");
            Console.WriteLine(account);
            if (account != null)
            {
                Console.WriteLine($"username2: {model.UserNameOrEmail}");
                // تشفير كلمة المرور المدخلة باستخدام SHA512
                string hashedInputPassword = HashPassword(model.Password);
                Console.WriteLine($"hashedInputPassword: {HashPassword(model.Password)}");

                // التحقق من أن القيمة المشفرة تطابق القيمة المخزنة
                    
                if (hashedInputPassword == account.Passwords)
                {
                    var role = _context.Acounts.FirstOrDefault(s => s.Email == account.Email);

                    if (role.Role == "admin")
                    {
                        var manager = _context.Menegars.FirstOrDefault(s => s.Email == account.Email);
                        HttpContext.Session.SetString("UserName", account.UsersName);
                        HttpContext.Session.SetString("Role", account.Role);
                        HttpContext.Session.SetString("Email", account.Email);
                        HttpContext.Session.SetString("Id", manager.Id.ToString());
                    }
                    else if (role.Role == "Teacher")
                    {
                        var teacher = _context.Teachers.FirstOrDefault(s => s.Email == account.Email);
                        HttpContext.Session.SetString("UserName", account.UsersName);
                        HttpContext.Session.SetString("Role", account.Role);
                        HttpContext.Session.SetString("Email", account.Email);
                        HttpContext.Session.SetString("Id", teacher.Id.ToString());
                    }
                    else if (role.Role == "Student")
                    {
                        var Student = _context.Students.FirstOrDefault(s => s.Email == account.Email);
                        HttpContext.Session.SetString("UserName", account.UsersName);
                        HttpContext.Session.SetString("Role", account.Role);
                        HttpContext.Session.SetString("Email", account.Email);
                        HttpContext.Session.SetString("Id", Student.Id.ToString());
                    }
                    
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, account.UsersName),
                        new Claim(ClaimTypes.Email, account.Email),
                        new Claim(ClaimTypes.Role, account.Role)                    };

                    var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var principal = new ClaimsPrincipal(identity);

                    // تسجيل الدخول باستخدام ملفات تعريف الارتباط
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
                    
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    ModelState.AddModelError("", "Username or password is invalid.");
                }
            }
            else
            {
                ModelState.AddModelError("", "Username or password is invalid.");
            }
        }
        return View(model);
    }

    // GET: /Account/Register
    public IActionResult Register(string email, int phone, string fullname)
    {
        if (chick_user())
        {
            // إذا كان المستخدم مصادقًا عليه، قم بإعادة توجيهه إلى الصفحة الرئيسية
            return RedirectToAction("Index", "Home");
        }
        var model = new RegisterViewModel
        {
            Email = email,
            Phone = phone,
            FullName = fullname
        };
        return View(model);
    }

    // POST: /Account/Register
    [HttpPost]
    public IActionResult Register(RegisterViewModel model)
    {
        if (ModelState.IsValid)
        {
            var student = _context.Students.FirstOrDefault(s => s.Email == model.Email && s.Phone == model.Phone && s.Name == model.FullName);
            if (student != null)
            {
                return RedirectToAction("SetCredentials", new { email = model.Email, role = "Student" });
            }

            var teacher = _context.Teachers.FirstOrDefault(t => t.Email == model.Email && t.Phone == model.Phone && t.Name == model.FullName);
            if (teacher != null)
            {
                return RedirectToAction("SetCredentials", new { email = model.Email, role = "Teacher" });
            }

            var manager = _context.Menegars.FirstOrDefault(m => m.Email == model.Email && m.Phone == model.Phone && m.Name == model.FullName);
            if (manager != null)
            {
                return RedirectToAction("SetCredentials", new { email = model.Email, role = "admin" });
            }

            ModelState.AddModelError("", "The information you entered is incorrect.");
        }
        return View(model);
    }
    // GET: /Account/SetCredentials
    [HttpGet]
    public IActionResult SetCredentials(string email, string role)
    {
        if (chick_user())
        {
            // إذا كان المستخدم مصادقًا عليه بالفعل، قم بإعادة توجيهه إلى الصفحة الرئيسية
            return RedirectToAction("Index", "Home");
        }

        // إنشاء نموذج لعرض الصفحة
        var model = new SetCredentialsViewModel
        {
            Email = email,
            Role = role
        };

        // عرض الصفحة مع النموذج
        return View(model);
    }

    // POST: /Account/SetCredentials
    [HttpPost]
    public async Task<IActionResult> SetCredentials(SetCredentialsViewModel model)
    {
        // التحقق من صحة النموذج
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        // تشفير كلمة المرور باستخدام SHA512
        string hashedPassword = HashPassword(model.Password);
        string hashedConfirmPassword = HashPassword(model.ConfirmPassword);

        // التحقق مما إذا كانت كلمة المرور وتأكيد كلمة المرور متطابقتين
        if (hashedPassword != hashedConfirmPassword)
        {
            ModelState.AddModelError("", "The password and confirmation password do not match.");
            return View(model);
        }

        // التحقق مما إذا كان اسم المستخدم أو البريد الإلكتروني مستخدمًا بالفعل
        var account1 = _context.Acounts.FirstOrDefault(a => a.UsersName == model.UserName || a.Email == model.Email);
        if (account1 != null)
        {
            ModelState.AddModelError("", "The username or email is already in use.");
            return View(model);
        }

        // إنشاء حساب جديد
        var account = new Acount
        {
            UsersName = model.UserName,
            Passwords = hashedPassword, // تخزين القيمة المشفرة
            Email = model.Email,
            Role = model.Role,
            ResetToken = " ",
            ResetTokenExpiry = System.DateTime.Now
        };

        // إضافة الحساب إلى قاعدة البيانات
        _context.Add(account);
        _context.SaveChanges();

        // إنشاء مطالبات الهوية (Claims)
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, account.UsersName),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, account.Role) // إضافة الدور إلى الهوية
        };

        // إنشاء هوية المستخدم
        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

        // تسجيل الدخول باستخدام ملفات تعريف الارتباط
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity));

        // تخزين بيانات المستخدم في الجلسة (اختياري)
        HttpContext.Session.SetString("UserName", account.UsersName);
        HttpContext.Session.SetString("Role", account.Role);
        HttpContext.Session.SetString("Email", account.Email);

        // إعادة توجيه المستخدم إلى الصفحة الرئيسية بعد تسجيل الدخول
        return RedirectToAction("Index", "Home");
    }
    public async Task<IActionResult> Logout()
    {
        HttpContext.Session.Clear(); // مسح الجلسة
        // تسجيل الخروج باستخدام ملفات تعريف الارتباط
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // إعادة توجيه المستخدم إلى صفحة تسجيل الدخول
        return RedirectToAction("Login");
    }

    public IActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
    {
        Console.WriteLine($"Email: {model.Email}");
        if (ModelState.IsValid)
        {
            
            Console.WriteLine(ModelState.IsValid);
            var account = _context.Acounts.FirstOrDefault(a => a.Email == model.Email);
            if (account != null)
            {
                // إنشاء رمز تحقق عشوائي
                var resetToken = Guid.NewGuid().ToString();
                var tokenExpiry = DateTime.UtcNow.AddHours(1); // صلاحية الرمز لمدة ساعة
                // التحقق من أن الأعمدة ليست NULL
                if (account.ResetToken == null || account.ResetTokenExpiry == null)
                {
                    account.ResetToken = resetToken;
                    account.ResetTokenExpiry = tokenExpiry;
                }
                else
                {
                    // إذا كان هناك رمز سابق، يمكن تحديثه
                    account.ResetToken = resetToken;
                    account.ResetTokenExpiry = tokenExpiry;
                }

                _context.SaveChanges();

                // إنشاء رابط إعادة تعيين كلمة المرور
                var resetLink = Url.Action("ResetPassword", "Account", new { token = resetToken }, Request.Scheme);

                // إرسال البريد الإلكتروني
                await SendResetEmailAsync(model.Email, resetLink);
            }
            Console.WriteLine($"Email: {model.Email}");

            // لا نخبر المستخدم إذا كان البريد غير موجود لتجنب تسريب المعلومات
            TempData["Message"] = "If the email exists, a reset link has been sent.";
            return RedirectToAction("Login");
        }
        return View(model);
    }

    private async Task SendResetEmailAsync(string email, string resetLink)
    {
        var subject = "Password Reset Request";
        var body = $"Please click the following link to reset your password: {resetLink}";

        // استخدام Mailtrap لإرسال البريد الإلكتروني
        using (var smtpClient = new MailKit.Net.Smtp.SmtpClient())
        {
            await smtpClient.ConnectAsync(
                Environment.GetEnvironmentVariable("MAILTRAP_HOST"),
                int.Parse(Environment.GetEnvironmentVariable("MAILTRAP_PORT")),
                false);

            smtpClient.Authenticate(
                Environment.GetEnvironmentVariable("MAILTRAP_USERNAME"),
                Environment.GetEnvironmentVariable("MAILTRAP_PASSWORD"));

            var mailMessage = new MimeKit.MimeMessage();
            mailMessage.From.Add(new MimeKit.MailboxAddress("Your App", "no-reply@example.com"));
            mailMessage.To.Add(new MimeKit.MailboxAddress("", email));
            mailMessage.Subject = subject;
            mailMessage.Body = new MimeKit.TextPart("plain") { Text = body };

            await smtpClient.SendAsync(mailMessage);
            await smtpClient.DisconnectAsync(true);
        }
    }

    [HttpGet]
    public IActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return BadRequest("Invalid token.");
        }

        var model = new ResetPasswordViewModel { Token = token };
        return View(model);
    }
    [HttpPost]
    public IActionResult ResetPassword(ResetPasswordViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var account = _context.Acounts.FirstOrDefault(a => a.ResetToken == model.Token && a.ResetTokenExpiry > DateTime.UtcNow);
        
        if (account == null)
        {
            return BadRequest("Invalid or expired token.");
        }
        string hashedPassword = HashPassword(model.NewPassword);
        string hashedConfirmPassword = HashPassword(model.ConfirmPassword);
        if (hashedPassword != hashedConfirmPassword)
        {
            ModelState.AddModelError("", "The password and confirmation password do not match.");
            return View(model);
        }

        account.Passwords = HashPassword(model.NewPassword);
    
        // مسح الرموز القديمة بعد تحديث كلمة المرور
        

        _context.SaveChanges();

        TempData["Message"] = "Your password has been successfully reset.";
        return RedirectToAction("Login");
    }


}