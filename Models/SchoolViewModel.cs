namespace SchoolSystem.Models
{

    public partial class SchoolViewModel
    {
        public int Id { get; set; }

        public string Name { get; set; } = null!;

        public int? IdStatusSchool { get; set; }

        public int? IdGender { get; set; }

        public bool? IsDeleted { get; set; }


    }
}