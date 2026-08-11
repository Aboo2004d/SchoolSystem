namespace SchoolSystem.Models.AdminSchool
{
    public class AddTeacherToClassLectuers
    {
        public Guid idTeacher { get; set; }
        public Guid idClass { get; set; }
        public List<Guid> lectuers { get; set; }
    }
    
}
