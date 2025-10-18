using System;
using System.IO;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using DotNetEnv;
using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using SchoolSystem.Data;
using SchoolSystem.Migrations;
using SchoolSystem.Controllers;
using QuestPDF;
using NuGet.Packaging;
using QuestPDF.Drawing;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// 📌 إعداد QuestPDF وخطوطه
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
FontManager.RegisterFont(File.OpenRead("wwwroot/Amiri/Amiri-Bold.ttf"));
FontManager.RegisterFont(File.OpenRead("wwwroot/Amiri/Amiri-BoldItalic.ttf"));
FontManager.RegisterFont(File.OpenRead("wwwroot/Amiri/Amiri-Italic.ttf"));
FontManager.RegisterFont(File.OpenRead("wwwroot/Amiri/Amiri-Regular.ttf"));

// 📁 تحميل المتغيرات البيئية من ملف .env
Env.Load("E:\\Uni\\Files\\Training\\aspdotnet_core\\SchoolSystem\\appsetting.env");

// 📦 تسجيل الخدمات الأساسية
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<EncryptionHelper>();

// 🧠 كاش الجلسة في الذاكرة المحلية فقط (جلسة مؤقتة)
builder.Services.AddDistributedMemoryCache(); 

// 🔌 إضافة DbContext مع سلسلة الاتصال
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SystemSchoolDbContext>(options => options.UseSqlServer(conn));

// 🔧 خدمات مخصصة
builder.Services.AddScoped<IErrorLoggerService, ErrorLoggerService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailValidationService, EmailValidationService>();
builder.Services.AddScoped<ISessionValidatorService, SessionValidatorService>();

// 🧠 إعداد الجلسات (Session) مؤقتة
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // انتهاء الجلسة بعد 30 دقيقة من عدم النشاط
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.MaxAge = null; // جلسة مؤقتة تمسح عند إغلاق المتصفح أو إعادة تشغيل السيرفر
});
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = ".SchoolSystem.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.IsEssential = true;

        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // مدة صلاحية الكوكي
        options.SlidingExpiration = true; // تمديد الوقت عند النشاط
        options.SessionStore = null; // تأكد أنه لا يستخدم أي تخزين دائم
    });

// 🧠 إضافة Redis فقط للكاش (لا علاقة له بالجلسات)
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";   // عنوان Redis
    options.InstanceName = "SchoolApp_";        // بادئة للكاش
});

// 🔒 إضافة خدمات التفويض
builder.Services.AddAuthorization();

// 🔔 إعداد إشعارات ToastNotification
builder.Services.AddNotyf(options =>
{
    options.Position = NotyfPosition.TopCenter;
    options.DurationInSeconds = 3;
    options.IsDismissable = true;
});

// 🗂 إنشاء مجلد خاص لتخزين الصور (إن لم يكن موجودًا)
var profileImageFolder = Path.Combine(Directory.GetCurrentDirectory(), "PrivateImages");
Directory.CreateDirectory(profileImageFolder);

// 🛠 بناء التطبيق
var app = builder.Build();

// 🌍 إعداد بيئة التشغيل
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();  // صفحة الأخطاء للمطورين
    app.UseHsts();                    // حماية HSTS
}

// 🔐 إعادة التوجيه إلى HTTPS
app.UseHttpsRedirection();

// 📂 تمكين تقديم الملفات الثابتة من wwwroot
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    RequestPath = ""
});

// 🧯 تسجيل أخطاء مخصصة عبر Middleware
app.UseMiddleware<ErrorHandlingMiddleware>();

// 🧭 التوجيه قبل المصادقة
app.UseRouting();

// 🔐 تفعيل المصادقة والتفويض
app.UseAuthentication();
app.UseAuthorization();

// 🧠 تفعيل الجلسات المؤقتة
app.UseSession();

// 🔔 تفعيل إشعارات ToastNotification
app.UseNotyf();

// ➡️ تعيين المسار الافتراضي للطلبات
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// 🧹 مسح مفاتيح Redis الخاصة بالتطبيق عند بدء التشغيل
var redis = ConnectionMultiplexer.Connect("localhost:6379");
var server = redis.GetServer("localhost", 6379);
foreach (var key in server.Keys(pattern: "SchoolApp_*"))
{
    redis.GetDatabase().KeyDelete(key);
}

// ▶️ تشغيل التطبيق
app.Run();
