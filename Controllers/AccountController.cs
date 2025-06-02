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
using AspNetCoreHero.ToastNotification.Notyf;
using AspNetCoreHero.ToastNotification.Abstractions;
using System.Threading.Tasks;
using SchoolSystem.Filters;
namespace SchoolSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SystemSchoolDbContext _context;
        private readonly INotyfService _notyf;
        private readonly IErrorLoggerService _logger;
        private readonly IAccountService _accountService;


        public AccountController(SystemSchoolDbContext context, INotyfService notyf, IErrorLoggerService logger, IAccountService accountService)
        {
            _logger = logger;
            _context = context;
            _notyf = notyf;
            _accountService = accountService;
        }

        public bool CheckUser()
        {
            return User.Identity.IsAuthenticated &&
                HttpContext.Session.GetInt32("Id") != null &&
                HttpContext.Session.GetString("UserName") != null &&
                HttpContext.Session.GetString("Role") != null;
        }

        private readonly int SaltSize = 16; // حجم الملح بالبايت (128 بت)
        private readonly int KeySize = 32;  // حجم المفتاح الناتج (256 بت)
        private readonly int Iterations = 10000; // عدد التكرارات

        public string HashPassword(string password)
        {
            // إنشاء ملح عشوائي
            using (var rng = RandomNumberGenerator.Create())
            {
                byte[] salt = new byte[SaltSize];
                rng.GetBytes(salt);

                // توليد الهاش باستخدام PBKDF2
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                {
                    byte[] key = pbkdf2.GetBytes(KeySize);

                    // تخزين الملح والهاش معًا في صيغة Base64 مفصولة بفاصل (مثل $)
                    return $"{Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
                }
            }
        }

        public bool VerifyPassword(string password, string storedHash)
        {
            // فصل الملح والهاش المخزن
            var parts = storedHash.Split('$');
            if (parts.Length != 2)
                return false;

            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] key = Convert.FromBase64String(parts[1]);

            // توليد هاش جديد من كلمة المرور المدخلة والملح نفسه
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                byte[] keyToCheck = pbkdf2.GetBytes(KeySize);

                // مقارنة بين الهاش المخزن والهاش الذي تم توليده
                return CryptographicOperations.FixedTimeEquals(key, keyToCheck);
            }
        }
        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (CheckUser())
            {
                Console.WriteLine($"CheckUser: {CheckUser()}");
                // إذا كان المستخدم مصادقًا عليه، قم بإعادة توجيهه إلى الصفحة الرئيسية
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                Acount? account = await _context.Acounts
                .FirstOrDefaultAsync(a => a.UsersName == model.UserName);

                if (account != null)
                {
                    if (VerifyPassword(model.Password, account.Passwords))
                    {
                        if (account.IsActive == true)
                        {
                            switch (account.Role)
                            {
                                case "admin":
                                    Menegar? menegar = await _context.Menegars.FirstOrDefaultAsync(s => s.Id == account.IdUser);
                                    if (await Cookies(account.UsersName, account.Role, menegar?.Email, menegar?.Id, menegar?.IdSchool, menegar?.Name))
                                    {
                                        return RedirectToAction("Index", "Home");
                                    }
                                    break;
                                case "Student":
                                    Student? student = await _context.Students.FirstOrDefaultAsync(s => s.Id == account.IdUser);
                                    if (await Cookies(account.UsersName, account.Role, student?.Email, student?.Id, student?.IdSchool, student?.Name))
                                    {
                                        return RedirectToAction("Index", "Home");
                                    }
                                    break;
                                case "Teacher":
                                    Teacher? teacher = await _context.Teachers.FirstOrDefaultAsync(s => s.Id == account.IdUser);
                                    if (await Cookies(account.UsersName, account.Role, teacher?.Email, teacher?.Id, teacher?.IdSchool, teacher?.Name))
                                    {
                                        return RedirectToAction("Index", "Home");
                                    }
                                    break;
                                default:
                                    _notyf.Error("فشل تسجيل الدخول");
                                    break;
                            }
                        }
                        else
                        {
                            _notyf.Error("فشل تسجيل الدخول");
                        }
                    }
                    else
                    {
                        _notyf.Error("اسم المستخدم او كلمة المرور خاظئة");
                    }
                }
                else
                {
                    _notyf.Error("اسم المستخدم او كلمة المرور خاظئة");
                }
            }
            else
            {
                _notyf.Error("خطأ في البيانات المدخلة");
            }
            return View(model);
        }

        protected async Task<bool> Cookies(string? username, string? role, string? email, int? id, int? school, string? name)
        {
            // تحقق من القيم المدخلة
            if (string.IsNullOrWhiteSpace(username) || 
                string.IsNullOrWhiteSpace(role) || 
                string.IsNullOrWhiteSpace(email) || 
                id == null || 
                school == null || 
                string.IsNullOrWhiteSpace(name))
            {
                _notyf.Error("فشل تسجيل الدخول. يرجى المحاولة مرة أخرى.");
                return false;
            }

            // ✅ تخزين القيم الأساسية فقط في الجلسة
            HttpContext.Session.SetInt32("Id", id??0);
            HttpContext.Session.SetInt32("School", school??0);
            HttpContext.Session.SetString("UserName", username);
            HttpContext.Session.SetString("Role", role);
            
            // ✅ إعداد قائمة Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role),
                new Claim(ClaimTypes.Email, email)
            };

            // ✅ إنشاء الهوية والمستخدم
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // ✅ خيارات الكوكيز
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = false, // تبقى بعد إغلاق المتصفح
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            // ✅ تسجيل الدخول
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, authProperties);

            // ✅ ترحيب بالاسم الأول
            var firstName = name.Split(" ")[0];
            _notyf.Success($"مرحباً {firstName}");
            return true;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            if (CheckUser())
            {
                // إذا كان المستخدم مصادقًا عليه، قم بإعادة توجيهه إلى الصفحة الرئيسية
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _accountService.RegisterUserAsync(model);
                if (!result.IsSuccess)
                {
                    _notyf.Error(result.Message);
                    return View(model);
                }
                HttpContext.Session.SetString("email", model.Email);
                HttpContext.Session.SetString("role", model.Role);
                HttpContext.Session.SetInt32("idUser", model.IdUser);
                HttpContext.Session.SetInt32("school", model.School ?? 0);
                HttpContext.Session.SetString("name", model.FullName);

                return RedirectToAction("SetCredentials");
            }
            
            Console.WriteLine($"Name: {model.FullName}");
            _notyf.Error("بيانات التحقق غير صحيحة");
            return View(model);

        }
        // GET: /Account/SetCredentials
        [HttpGet]
        public async Task<IActionResult> SetCredentials()
        {
            if (CheckUser())
            {
                return RedirectToAction("Index", "Home");
            }
            string? email = HttpContext.Session.GetString("email");
            string? role = HttpContext.Session.GetString("role");
            int? idUser = HttpContext.Session.GetInt32("idUser");
            int? school = HttpContext.Session.GetInt32("school");
            string? name = HttpContext.Session.GetString("name");

            if (email == null || role == null || idUser == null || school == null)
            {
                _notyf.Error("البيانات غير متوفرة أو انتهت الجلسة");
                return RedirectToAction("Register");
            }
            Console.WriteLine($"Role: {role}");

            var model = new SetCredentialsViewModel
            {
                Email = email,
                Role = role,
                IdUser = idUser ?? 0,
                School = school ?? 0,
                name = name ?? "Null"
            };
            HttpContext.Session.Clear();
            return View(model);
        }

        // POST: /Account/SetCredentials
        [HttpPost]
        public async Task<IActionResult> SetCredentials(SetCredentialsViewModel model)
        {

            // التحقق من صحة النموذج
            if (!ModelState.IsValid)
            {
                foreach (var state in ModelState)
                {
                    var key = state.Key;
                    var errors = state.Value.Errors;
                    
                    foreach (var error in errors)
                    {
                        Console.WriteLine($"خطأ في الحقل: {key} - الرسالة: {error.ErrorMessage}");
                    }
                }
                _notyf.Error("خطأ بالبيانات المدخلة");
                return View(model);
            }

            // التحقق مما إذا كانت كلمة المرور وتأكيد كلمة المرور متطابقتين
            if (model.Password != model.ConfirmPassword)
            {
                _notyf.Error("كلمة المرور غير متطابقة");
                return View(model);
            }

            // التحقق مما إذا كان اسم المستخدم مستخدمًا بالفعل
            bool account1 = _context.Acounts.Any(a => a.UsersName == model.UserName);
            if (account1 == true)
            {
                _notyf.Error("اسم المستخدم موجود مسبقا");
                return View(model);
            }

            // إنشاء حساب جديد
            Acount account = new Acount
            {
                UsersName = model.UserName,
                Passwords = HashPassword(model.Password), // تخزين القيمة المشفرة
                Email = model.Email,
                IdUser = model.IdUser,
                Role = model.Role,
                ResetToken = " ",
                IsActive = true,
                ResetTokenExpiry = System.DateTime.Now
            };

            // إضافة الحساب إلى قاعدة البيانات
            await _context.Acounts.AddAsync(account);
            await _context.SaveChangesAsync();

            if (await Cookies(account.UsersName, account.Role, account.Email, model.IdUser, model.School, model.name))
                return RedirectToAction("Index", "Home");
            return await Logout();
        }

        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear(); // مسح الجلسة
            // تسجيل الخروج باستخدام ملفات تعريف الارتباط
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _notyf.Success("تم تسجيل الخروج");
            // إعادة توجيه المستخدم إلى صفحة تسجيل الدخول
            return RedirectToAction("Login");
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            if (CheckUser())
            {
                _notyf.Warning("The user is authenticated!");
                // إذا كان المستخدم مصادقًا عليه، قم بإعادة توجيهه إلى الصفحة الرئيسية
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            Console.WriteLine($"Email: {model.Email}");
            if (ModelState.IsValid)
            {

                Console.WriteLine(ModelState.IsValid);
                Acount? account = _context.Acounts.FirstOrDefault(a => a.Email == model.Email);
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

                _notyf.Success($"An email to reset the password has been sent to the email {model.Email}.");
                return RedirectToAction("Login");
            }
            _notyf.Error("There is an error in the entered data.");
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
            if (CheckUser())
            {
                _notyf.Warning("The user is authenticated!");
                return RedirectToAction("Index", "Home");
            }
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

            if (model.NewPassword != model.ConfirmPassword)
            {
                _notyf.Error("كلمة المرور المطابقة غير صحيحة");
                return View(model);
            }
            if (account.Passwords == hashedPassword)
            {
                _notyf.Error("لا يمكن ان تكون كلمة المرور الجديدة تشبه القديمة");
                return View(model);
            }

            account.Passwords = HashPassword(model.NewPassword);

            // مسح الرموز القديمة بعد تحديث كلمة المرور


            _context.SaveChanges();

            _notyf.Success("تم تحديث كلمة المرور بنجاح");
            return RedirectToAction("Login");
        }



        [HttpGet]
        [AuthorizeRoles("admin", "Student", "Teacher")]
        public IActionResult NewPassword()
        {

            return View();
        }

        [HttpPost]
        [AuthorizeRoles("admin", "Student", "Teacher")]
        public async Task<IActionResult> NewPassword(NewPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                _notyf.Error("خطأ بالبيانات المدخلة");
                return View(model);
            }
            string username = HttpContext.Session.GetString("UserName") ?? "null";
            if (username == "null")
            {
                return await Logout();
            }
            Acount? account = await _context.Acounts.FirstOrDefaultAsync(acc => username == acc.UsersName);
            if (account == null)
            {
               return await Logout();
            }
            string Hash = HashPassword(model.LastPassword);
            if (Hash != account.Passwords)
            {
                _notyf.Error("كلمة المرور القديمة خاطئة");
                return View(model);
            }
            if (model.NewPassword != model.ConfirmPassword)
            {
                _notyf.Error("كلمة المرور المدخلة للتأكيد خاطئة");
                return View(model);
            }
            if (HashPassword(model.NewPassword) == Hash)
            {
                _notyf.Error("لا يمكن ان تكون كلمة المرور القديمة تشبه الجديدة");
                return View(model);
            }
            account.Passwords = HashPassword(model.NewPassword);
            _context.SaveChanges();
            _notyf.Success("تم تحديث كلمة المرور");
            return RedirectToAction("IndexProfile", "Profile");
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            _notyf.Warning("لا تملك الصلاحية للوصول إلى هذه الصفحة.");
            return RedirectToAction("Index", "Home");
        }

    }
}