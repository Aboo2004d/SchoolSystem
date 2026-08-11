namespace SchoolSystem.Models{
    public class PostCreateTeacherToClass
    {
        public Guid idClass { get; set; }
        public Guid idTeacher { get; set; }
        public Guid? idLectuer { get; set; }
        
    }
}