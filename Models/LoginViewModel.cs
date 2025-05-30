using System.ComponentModel.DataAnnotations;
namespace SchoolSystem.Models{
    public class LoginViewModel
    {
        public string UserName { get; set; }
        public string Password { get; set; } // كلمة المرور
    }
}
