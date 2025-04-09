namespace SchoolSystem.Models
{
    public class LoginAccountViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; } // أضيفت هذه السطر
        public int Phone { get; set; }
        public string Role { get; set; }
    }
}