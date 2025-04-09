namespace SchoolSystem.Models{
    public class ManagerMenegarStudentInClassViewModel
    {
        public int Id { get; set; }
        public int IdStudent { get; set; }
        public int IdClass { get; set; }
        
        public string StudentName { get; set; }
        public string ClassroomName { get; set; } 
        
    }

}