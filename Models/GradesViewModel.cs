namespace SchoolSystem.Model
{

    public partial class GradesViewModel
    {
        public int Id { get; set; }

        public int? FirstMonth { get; set; }

        public int? Mid { get; set; }

        public int? SecondMonth { get; set; }

        public int? Activity { get; set; }

        public int? Final { get; set; }
        
        public int? Total { get; set; }

        public int? IdStudent { get; set; }
        public string? StudentName { get; set; }

        public int? IdTeacher { get; set; }
        public string? TeacherName { get; set; }

        public int? IdLectuer { get; set; }

        public string? LectuerName { get; set; }

        public int? IdClass { get; set; }
        public string? ClassroomName { get; set; }
    }
}