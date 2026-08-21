using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models;
public sealed class ResetStudentPasswordRequest
{
    [Required, RegularExpression(@"^[1-9][0-9]{8}$")] public string IdentityNumber { get; set; } = string.Empty;
}
