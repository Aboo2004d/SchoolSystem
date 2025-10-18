namespace SchoolSystem.Models
{
    public partial class TheClassViewModel
    {
        public string Id { get; set; }

        public string Name { get; set; } = null!;

        public int? IdSchool { get; set; }

        public bool? IsDeleted { get; set; }
    }
    
}

