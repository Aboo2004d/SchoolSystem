using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Models{
    public class ForgotPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; }
}
}