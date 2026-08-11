namespace SchoolSystem.Models{
    public class ManagerMenegarStudentInClassViewModel
    {
        public Guid Id { get; set; }
        public Guid? IdStudent { get; set; }
        public Guid? IdClass { get; set; }
        public Guid? LastIdClass { get; set; }
        
        public string? StudentName { get; set; }
        public string? ClassroomName { get; set; } 
        public string? LectuerName { get; set; } 
        
    }

}