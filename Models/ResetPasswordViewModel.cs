using System.ComponentModel.DataAnnotations;
namespace SchoolSystem.Models{
  public class ResetPasswordViewModel
    {
        [Required]
        public string Token { get; set; }
        public string NewPassword { get; set; }
        public string ConfirmPassword { get; set; }
    }  
    }
