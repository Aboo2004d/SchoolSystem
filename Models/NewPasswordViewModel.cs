using System.ComponentModel.DataAnnotations;
namespace SchoolSystem.Models{
  public class NewPasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        public string LastPassword { get; set; }

        [Required, DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [Required, DataType(DataType.Password), Compare(nameof(NewPassword))]
        public string ConfirmPassword { get; set; }
  }  
}
