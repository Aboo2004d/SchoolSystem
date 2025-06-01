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

var builder = WebApplication.CreateBuilder(args);

// 📁 تحميل المتغيرات البيئية من ملف .env
Env.Load("E:\\Uni\\Files\\Training\\aspdotnet_core\\SchoolSystem\\appsetting.env");

// 📦 تسجيل الخدمات
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// 🧠 كاش الجلسة لتوزيعها (مستقبلاً يمكن استبدالها بـ Redis)
builder.Services.AddDistributedMemoryCache();

// 🔌 إضافة DbContext مع سلسلة الاتصال
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SystemSchoolDbContext>(options => options.UseSqlServer(conn));
builder.Services.AddScoped<IErrorLoggerService, ErrorLoggerService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailValidationService, EmailValidationService>();
builder.Services.AddScoped<ISessionValidatorService, SessionValidatorService>();

// 🧠 إعداد الجلسات (Session)
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.MaxAge = null;   
});

// 🔐 إعداد المصادقة (Authentication) والتفويض (Authorization)
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
    });

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
    app.UseDeveloperExceptionPage();  // للمطورين
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

// 🧭 التوجيه أولاً قبل المصادقة
app.UseRouting();

// 🔐 تفعيل المصادقة والتفويض
app.UseAuthentication();
app.UseAuthorization();

// 🧠 تفعيل الجلسات
app.UseSession();

// 🔔 تفعيل إشعارات ToastNotification
app.UseNotyf();

// ➡️ تعيين المسار الافتراضي للطلبات
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

// ▶️ تشغيل التطبيق
app.Run();
