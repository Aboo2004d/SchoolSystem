namespace SchoolSystem.Models
{

    public partial class TeacherLectuerClassViewModel
    {
        public Guid Id { get; set; }

        public Guid? IdTeacher { get; set; }

        public Guid? IdLectuer { get; set; }

        public Guid? IdSchool { get; set; }

        public Guid? IdClass { get; set; }

        public bool? IsDeleted { get; set; }
    }
}