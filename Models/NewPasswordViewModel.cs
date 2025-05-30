using System.ComponentModel.DataAnnotations;
namespace SchoolSystem.Models{
  public class NewPasswordViewModel
    {
        [DataType(DataType.Password)]
        public string LastPassword { get; set; }

        [DataType(DataType.Password)]
        public string NewPassword { get; set; }

        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }
    }  
    }
