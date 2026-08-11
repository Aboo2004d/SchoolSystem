namespace SchoolSystem.Models{
    public class GetCreateTeacherToClass
    {
        public List<Teachers> teachers { get; set; }
        public List<Lectuers> lectuers { get; set; }
        public Guid idClass { get; set; }
        public string nameClass { get; set; }
        
    }

    public class Teachers{
        public Guid idTeacher { get; set; }
        public string nameTeacher { get; set; }

    }
    public class Lectuers{
        public Guid idLectuer { get; set; }
        public string nameLectuer { get; set; }

    }
}