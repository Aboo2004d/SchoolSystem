namespace SchoolSystem.Models
{
    public partial class TheClassViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public Guid? IdSchool { get; set; }

        public bool? IsDeleted { get; set; }
    }
    
}

