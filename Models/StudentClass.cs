namespace SchoolSystem.Models
{

    public partial class StudentClass
    {
        public int Id { get; set; }

        public int? IdStudent { get; set; }

        public int? IdClass { get; set; }

        public int? IdSchool { get; set; }

        public int? IdTeacher { get; set; }

        public int? IdLectuer { get; set; }
    }
}