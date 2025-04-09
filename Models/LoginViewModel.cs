using System.ComponentModel.DataAnnotations;
namespace SchoolSystem.Models{
    public class LoginViewModel
    {
        [Required]
        
        public string UserNameOrEmail { get; set; } // اسم المستخدم أو البريد

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } // كلمة المرور

        public bool RememberMe { get; set; } // تذكرني
    }
}
