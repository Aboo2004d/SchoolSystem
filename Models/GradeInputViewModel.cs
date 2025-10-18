namespace SchoolSystem.Models
{
    public class GradeInputViewModel
    {
        public string StudentId { get; set; }
        public string TeacherId { get; set; }
        public string LectuerId { get; set; }
        public string ClassId { get; set; }
        public int? FirstMonth { get; set; }
        public int? Mid { get; set; }
        public int? SecondMonth { get; set; }
        public int? Activity { get; set; }
        public int? Final { get; set; }
    }
}