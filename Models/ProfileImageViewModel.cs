namespace SchoolSystem.Models
{
    public partial class ProfileImageViewModel
    {
        public Guid Id { get; set; }

        public string? UserName { get; set; }

        public string? Email { get; set; }

        public string? ProfileImagePath { get; set; }

        public bool? IsDeleted { get; set; }
    }
}