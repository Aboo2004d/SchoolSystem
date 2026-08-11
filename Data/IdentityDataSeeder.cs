using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace SchoolSystem.Data;

public sealed class SeedAdminOptions
{
    public bool Enabled { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public int IdNumber { get; set; }
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public static class IdentityDataSeeder
{
    public static async Task SeedMainAdminAsync(IServiceProvider services, IConfiguration configuration)
    {
        var options = configuration.GetSection("SeedAdmin").Get<SeedAdminOptions>();
        if (options is null || !options.Enabled)
            return;

        Validate(options);

        var context = services.GetRequiredService<SystemSchoolDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await userManager.FindByNameAsync(options.UserName);
        var emailUser = await userManager.FindByEmailAsync(options.Email);
        if (user is not null && emailUser is not null && user.Id != emailUser.Id)
            throw new InvalidOperationException("SeedAdmin username and email belong to different users.");

        user ??= emailUser;
        if (user is null)
        {
            user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = options.UserName,
                Email = options.Email,
                EmailConfirmed = true,
                IsActive = true
            };
            EnsureSucceeded(await userManager.CreateAsync(user, options.Password), "create the main admin");
        }
        else
        {
            user.UserName = options.UserName;
            user.Email = options.Email;
            user.EmailConfirmed = true;
            user.IsActive = true;
            EnsureSucceeded(await userManager.UpdateAsync(user), "update the main admin");
        }

        if (!await userManager.IsInRoleAsync(user, RoleNames.Admin))
            EnsureSucceeded(await userManager.AddToRoleAsync(user, RoleNames.Admin), "assign the Admin role");

        var manager = await context.Menegars.SingleOrDefaultAsync(x =>
            x.ApplicationUserId == user.Id || x.IdNumber == options.IdNumber);
        if (manager is null)
        {
            manager = new Menegar
            {
                ApplicationUserId = user.Id,
                Name = options.FullName,
                Email = options.Email,
                Phone = options.Phone,
                IdNumber = options.IdNumber,
                IdSchool = null,
                IsDeleted = false,
                IsDeletedSchool = false
            };
            context.Menegars.Add(manager);
        }
        else
        {
            if (manager.ApplicationUserId.HasValue && manager.ApplicationUserId != user.Id)
                throw new InvalidOperationException("The seeded manager profile is linked to another Identity user.");
            manager.ApplicationUserId = user.Id;
            manager.Name = options.FullName;
            manager.Email = options.Email;
            manager.Phone = options.Phone;
            manager.IdNumber = options.IdNumber;
            manager.IsDeleted = false;
        }

        await context.SaveChangesAsync();
    }

    private static void Validate(SeedAdminOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.UserName) || string.IsNullOrWhiteSpace(options.Email) ||
            string.IsNullOrWhiteSpace(options.FullName) || string.IsNullOrWhiteSpace(options.Phone) ||
            string.IsNullOrWhiteSpace(options.Password) || options.IdNumber <= 0)
            throw new InvalidOperationException(
                "SeedAdmin configuration is incomplete. Store the password with: " +
                "dotnet user-secrets set \"SeedAdmin:Password\" \"<password>\"");
    }

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (result.Succeeded) return;
        throw new InvalidOperationException($"Failed to {operation}: " +
            string.Join("; ", result.Errors.Select(error => error.Description)));
    }
}
