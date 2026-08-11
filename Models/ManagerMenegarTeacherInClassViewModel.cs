namespace SchoolSystem.Models{
    public class ManagerMenegarTeacherInClassViewModel
    {
        public Guid Id { get; set; }
        public Guid IdTeacher { get; set; }
        public Guid IdClass { get; set; }
        public Guid IdLectuer { get; set; }
        public string LectuerName { get; set; }
        public string TeacherName { get; set; }
        public string ClassroomName { get; set; } 
        
    }

}