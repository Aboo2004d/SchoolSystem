using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models;

public sealed class DirectoratePersonRequest
{
    [Required, StringLength(150, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, RegularExpression(@"^[1-9][0-9]{8}$", ErrorMessage = "رقم الهوية يجب أن يتكون من 9 أرقام.")]
    public string IdNumber { get; set; } = string.Empty;

    [Required, Phone, StringLength(30)]
    public string Phone { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public DateOnly? BirthDate { get; set; }

    [Required, StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Area { get; set; } = string.Empty;

    [Required]
    public Guid? SchoolId { get; set; }

    public Guid? ClassId { get; set; }
}

