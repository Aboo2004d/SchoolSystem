namespace SchoolSystem.Models
{
    public class GradeInputViewModel
    {
        public Guid StudentId { get; set; }
        public Guid TeacherId { get; set; }
        public Guid LectuerId { get; set; }
        public Guid ClassId { get; set; }
        public int? FirstMonth { get; set; }
        public int? Mid { get; set; }
        public int? SecondMonth { get; set; }
        public int? Activity { get; set; }
        public int? Final { get; set; }
    }
}