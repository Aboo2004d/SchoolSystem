using Microsoft.AspNetCore.Identity;

namespace SchoolSystem.Data;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public bool IsActive { get; set; } = true;
    public Student? Student { get; set; }
    public Teacher? Teacher { get; set; }
    public Menegar? Menegar { get; set; }
    public DirectorateManager? DirectorateManager { get; set; }
    public MinistryManager? MinistryManager { get; set; }
}

public static class RoleNames
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string DirectorateManager = "DirectorateManager";
    public const string MinistryManager = "MinistryManager";
    public const string Teacher = "Teacher";
    public const string Student = "Student";

    public static string? Normalize(string? role) => role?.Trim().ToLowerInvariant() switch
    {
        "admin" => Admin,
        "manager" or "menegar" => Manager,
        "directoratemanager" or "directorate" => DirectorateManager,
        "ministrymanager" or "ministry" => MinistryManager,
        "teacher" => Teacher,
        "student" => Student,
        _ => null
    };
}
