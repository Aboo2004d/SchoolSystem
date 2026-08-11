namespace SchoolSystem.Models
{

    public partial class SchoolViewModel
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = null!;

        public Guid? IdStatusSchool { get; set; }

        public Guid? IdGender { get; set; }

        public bool? IsDeleted { get; set; }


    }
}