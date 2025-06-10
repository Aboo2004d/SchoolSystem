namespace SchoolSystem.Models
{
    public class EditProfileViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string UserName { get; set; }
        public string Phone { get; set; }
        public string Role { get; set; }
        public int IdNumber { get; set; }
        public DateOnly TheDate { get; set; }
        public string City { get; set; }
        public string Area { get; set; }
        public string School { get; set; }
        public string? TheClass { get; set; }
    }
}