namespace SchoolSystem.Models.AdminSchool
{
    public class StudentsTeachersInClassAtSchool
    {
        public Guid? idClass { get; set; }
        public string? nameClass { get; set; }
        public int numberOfStudents { get; set; }
        public int numberOfTeachers { get; set; }
        
    }

}