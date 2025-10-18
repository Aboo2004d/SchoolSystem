namespace SchoolSystem.Models
{

    public partial class TeacherLectuerClassViewModel
    {
        public int Id { get; set; }

        public int? IdTeacher { get; set; }

        public int? IdLectuer { get; set; }

        public int? IdSchool { get; set; }

        public int? IdClass { get; set; }

        public bool? IsDeleted { get; set; }
    }
}