namespace SchoolSystem.Models{
    public class LectuerInTeacherViewModel
    {
        public Guid Id { get; set; }
        public Guid? IdTeacher { get; set; }
        public Guid? IdLectuer { get; set; }
        public string? TeacherName { get; set; }
        public string? ClassroomName { get; set; } 
        public string? LectureName { get; set; } 
        
    }

}