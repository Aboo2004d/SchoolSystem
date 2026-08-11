using System.ComponentModel.DataAnnotations;
namespace SchoolSystem.Models{
  public class ResetPasswordViewModel
    {
        [Required]
        public Guid UserId { get; set; }
        [Required]
        public string Token { get; set; }
        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }
        [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; }
  }  
}
