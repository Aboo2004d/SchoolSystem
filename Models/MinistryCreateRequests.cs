using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models;

public sealed class MinistrySchoolRequest : DirectorateSchoolRequest
{
    [Required] public Guid? DirectorateId { get; set; }
}

public sealed class MinistryDirectorateRequest
{
    [Required, StringLength(30, MinimumLength = 2)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(200, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [StringLength(100)] public string? City { get; set; }
    [StringLength(100)] public string? Area { get; set; }
    [Phone, StringLength(30)] public string? Phone { get; set; }
    [EmailAddress, StringLength(256)] public string? Email { get; set; }
}
public sealed class MinistryPersonRequest
{
    [Required, StringLength(150, MinimumLength = 2)] public string Name { get; set; } = string.Empty;
    [Required, RegularExpression(@"^[1-9][0-9]{8}$")] public string IdNumber { get; set; } = string.Empty;
    [Required, Phone, StringLength(30)] public string Phone { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(200)] public string Email { get; set; } = string.Empty;
    [Required] public DateOnly? BirthDate { get; set; }
    [Required, StringLength(100)] public string City { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Area { get; set; } = string.Empty;
    [Required] public Guid? OrganizationId { get; set; }
    public Guid? ClassId { get; set; }
}

