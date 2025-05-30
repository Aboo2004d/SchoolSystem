namespace SchoolSystem.Models
{
    public class ProfileViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int IdNumber { get; set; }
        public DateOnly TheDate { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; } // أضيفت هذه السطر
        public string Phone { get; set; }
        public string Role { get; set; }
        public string School { get; set; }
        public string? TheClass { get; set; }

        public string? PhotoPath { get; set; }
        public bool PhotoExists { get; set; }
    }
}