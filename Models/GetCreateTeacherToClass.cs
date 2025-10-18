namespace SchoolSystem.Models{
    public class GetCreateTeacherToClass
    {
        public List<Teachers> teachers { get; set; }
        public List<Lectuers> lectuers { get; set; }
        public string idClass { get; set; }
        public string nameClass { get; set; }
        
    }

    public class Teachers{
        public string idTeacher { get; set; }
        public string nameTeacher { get; set; }

    }
    public class Lectuers{
        public string idLectuer { get; set; }
        public string nameLectuer { get; set; }

    }
}