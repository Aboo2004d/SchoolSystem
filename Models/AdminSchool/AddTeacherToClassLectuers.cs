namespace SchoolSystem.Models.AdminSchool
{
    public class AddTeacherToClassLectuers
    {
        public string idTeacher { get; set; }
        public string idClass { get; set; }
        public List<string> lectuers { get; set; }
    }
    
}