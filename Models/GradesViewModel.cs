using SchoolSystem.Data;

namespace SchoolSystem.Model
{

    public partial class GradesViewModel
    {
        public Guid Id { get; set; }

        public int? FirstMonth { get; set; }

        public int? Mid { get; set; }

        public int? SecondMonth { get; set; }

        public int? Activity { get; set; }

        public int? Final { get; set; }
        
        public int? Total { get; set; }

        public Guid? IdStudent { get; set; }
        public string? StudentName { get; set; }

        public Guid? IdTeacher { get; set; }
        public string? TeacherName { get; set; }

        public Guid? IdLectuer { get; set; }

        public string? LectuerName { get; set; }

        public Guid? IdClass { get; set; }
        public string? ClassroomName { get; set; }
        public Student? IdStudentNavigation { get; set; }
    }
}