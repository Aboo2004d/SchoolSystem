using AspNetCoreHero.ToastNotification;
using AspNetCoreHero.ToastNotification.Extensions;
using DotNetEnv;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using QuestPDF.Drawing;
using SchoolSystem.Data;
using SchoolSystem.Controllers;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
foreach (var font in new[] { "Amiri-Bold.ttf", "Amiri-BoldItalic.ttf", "Amiri-Italic.ttf", "Amiri-Regular.ttf" })
    FontManager.RegisterFont(File.OpenRead(Path.Combine("wwwroot", "Amiri", font)));

// Keep environment-file support, but do not depend on a developer-specific absolute path.
if (File.Exists("appsetting.env"))
    Env.Load("appsetting.env");

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new Microsoft.AspNetCore.Mvc.AutoValidateAntiforgeryTokenAttribute());
    options.Filters.Add<SchoolSystem.Filters.OwnershipAuthorizationFilter>();
});
builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
builder.Services.AddDbContext<SystemSchoolDbContext>(options => options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 10;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.SignIn.RequireConfirmedAccount = false;
})
    .AddEntityFrameworkStores<SystemSchoolDbContext>()
    .AddClaimsPrincipalFactory<ApplicationClaimsPrincipalFactory>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = ".SchoolSystem.Identity";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.AddScoped<IErrorLoggerService, ErrorLoggerService>();
builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IEmailValidationService, EmailValidationService>();
builder.Services.AddScoped<ISessionValidatorService, SessionValidatorService>();
builder.Services.AddScoped<IAutomaticAccountService, AutomaticAccountService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
});

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "localhost:6379";
    options.InstanceName = "SchoolApp_";
});
builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect("localhost:6379"));
builder.Services.AddAuthorization(options =>
{
    var authenticatedActiveUser = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireClaim("active", "true")
        .Build();
    options.DefaultPolicy = authenticatedActiveUser;
    // Everything is private unless an action explicitly opts out with [AllowAnonymous].
    options.FallbackPolicy = authenticatedActiveUser;
});
builder.Services.AddNotyf(options =>
{
    options.Position = NotyfPosition.TopCenter;
    options.DurationInSeconds = 3;
    options.IsDismissable = true;
});

Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "PrivateImages"));

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.UseDeveloperExceptionPage();
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

try
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<SystemSchoolDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
    foreach (var role in new[] { RoleNames.Admin, RoleNames.MinistryManager, RoleNames.DirectorateManager, RoleNames.Manager, RoleNames.Teacher, RoleNames.Student })
        if (!await roleManager.RoleExistsAsync(role))
            await roleManager.CreateAsync(new IdentityRole<Guid>(role));

    // The official main administrator is the only production seed and is created only
    // when SeedAdmin:Enabled and its secure deployment settings are provided.
    await IdentityDataSeeder.SeedMainAdminAsync(scope.ServiceProvider, app.Configuration);

    // Synthetic load data is strictly opt-in and can never run outside Development.
    if (app.Environment.IsDevelopment())
        await LoadTestDataSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);

    if (args.Contains("--seed-only", StringComparer.OrdinalIgnoreCase))
    {
        app.Logger.LogInformation("Database and identity seed completed successfully.");
        return;
    }
}
catch (Exception exception)
{
    app.Logger.LogCritical(exception,
        "Database initialization failed. Verify the connection string and secure seed configuration.");
    return;
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    RequestPath = ""
});
app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseRouting();
app.UseSession(); // Legacy compatibility data only; authorization comes from Identity claims.
app.UseAuthentication();
app.UseAuthorization();
app.UseNotyf();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");
app.Run();

public partial class Program;

