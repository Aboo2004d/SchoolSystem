namespace SchoolSystem.Models{
    public class RegisterViewModel
    {
        public string FullName { get; set; }

        public string Email { get; set; }
        public string Phone { get; set; }

        public int IdNumber { get; set; }

        public string City { get; set; }

        public string Area { get; set; }

        public int? School { get; set; }
        public string Role { get; set; }
        public int IdUser { get; set; }

        public DateOnly TheDate{get;set;}
    }
}