namespace SchoolSystem.Models
{

    public partial class StudentClass
    {
        public Guid Id { get; set; }

        public Guid? IdStudent { get; set; }

        public Guid? IdClass { get; set; }

        public Guid? IdSchool { get; set; }

        public Guid? IdTeacher { get; set; }

        public Guid? IdLectuer { get; set; }
    }
}