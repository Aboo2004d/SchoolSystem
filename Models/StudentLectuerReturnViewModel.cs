namespace SchoolSystem.Models{
    public class StudentLectuerReturnViewModel
    {
        public Guid Id { get; set; }
        public string TeacherName { get; set; }
        public Guid IdStudent { get; set; }
        public string StudentName { get; set; }
        public string ClassroomName { get; set; } 
        public string LectureName { get; set; } 
    }

}