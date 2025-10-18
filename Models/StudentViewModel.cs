namespace SchoolSystem.Models
{

    public partial class StudentViewModel
    {
        public string Id { get; set; }

        public string? Name { get; set; }

        public string? Phone { get; set; }

        public string? Email { get; set; }

        public int? IdSchool { get; set; }
        public string? SchoolName { get; set; }

        public DateOnly? TheDate { get; set; }

        public int? IdClass { get; set; }
        public string? ClassName { get; set; }

        public string? City { get; set; }
        public string? Address { get; set; }

        public string? Area { get; set; }

        public int? IdNumber { get; set; }

        public bool? IsDeleted { get; set; }
    }
}