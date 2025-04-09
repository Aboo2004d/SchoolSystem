using System.ComponentModel.DataAnnotations.Schema;
namespace SchoolSystem.Models{
    [Table("Acounts")]
    public class Account
    {
        public int Id { get; set; }
        
        [Column("UsersName")]
        public string UserName { get; set; } // اسم المستخدم
        
        [Column("Passwords")]
        public string Password { get; set; } // كلمة المرور
        public string Email { get; set; }    // البريد الإلكتروني
        public string Role { get; set; }     // الدور ("Admin", "Teacher", "Student")
        public string ResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }
    }
}
 