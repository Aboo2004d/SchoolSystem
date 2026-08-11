namespace SchoolSystem.Models{
    public class LectuerInStudentViewModel
    {
        public Guid Id { get; set; }
        public Guid? IdLectuer { get; set; }
        public string? TeacherName { get; set; }
        public Guid? IdTeacher { get; set; }
        public string? StudentName { get; set; }
        public string? ClassroomName { get; set; } 
        public string? LectureName { get; set; }
        
    }

}