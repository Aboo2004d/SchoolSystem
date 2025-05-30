namespace SchoolSystem.Models{
    public class ManagerMenegarTeacherInClassViewModel
    {
        public int Id { get; set; }
        public int IdTeacher { get; set; }
        public int IdClass { get; set; }
        public int IdLectuer { get; set; }
        public string LectuerName { get; set; }
        public string TeacherName { get; set; }
        public string ClassroomName { get; set; } 
        
    }

}