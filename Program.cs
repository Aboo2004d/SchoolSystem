using Microsoft.EntityFrameworkCore;
using SchoolSystem.Data;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// تحميل المتغيرات البيئية من ملف .env
Env.Load("E:\\Uni\\Files\\Training\\aspdotnet_core\\SchoolSystem\\appsetting.env");

// إضافة الخدمات إلى الحاوية
builder.Services.AddControllersWithViews();
builder.Services.AddHttpContextAccessor();

// إضافة DbContext باستخدام سلسلة الاتصال من التكوين
var conn = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<SystemSchoolDbContext>(options => options.UseSqlServer(conn));

// إضافة الجلسات
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // مدة انتهاء الجلسة
});

// إضافة خدمات المصادقة والتوصيات
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login"; // صفحة تسجيل الدخول
        options.AccessDeniedPath = "/Home/Index"; // إعادة التوجيه إلى الصفحة الرئيسية عند الوصول الممنوع
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30); // مدة صلاحية ملف تعريف الارتباط
        options.SlidingExpiration = true; // تمديد صلاحية ملف تعريف الارتباط تلقائيًا
    });

builder.Services.AddAuthorization();

// بناء التطبيق
var app = builder.Build();

// تكوين خط الأنابيب HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    //app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // استخدام HSTS لتحسين الأمان
}

// استخدام الجلسات والمصادقة والتوصيات
app.UseSession();
app.Use(async (context, next) =>
{
    // مسح ملف تعريف الارتباط إذا كان الموقع يُفتح لأول مرة
    if (context.Request.Cookies.ContainsKey(".AspNetCore.Cookies"))
    {
        context.Response.Cookies.Delete(".AspNetCore.Cookies");
    }
    await next();
});
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // تفعيل المصادقة
app.UseAuthorization();  // تفعيل التوصيات

// تعيين المسار الافتراضي
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();