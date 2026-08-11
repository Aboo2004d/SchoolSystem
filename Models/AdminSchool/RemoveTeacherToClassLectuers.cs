namespace SchoolSystem.Models.AdminSchool
{
    public class RemoveTeacherToClassLectuers
    {
        public Guid idTeacher { get; set; }
        public Guid idClass { get; set; }
        public Guid idLectuer { get; set; }
    }
    
}