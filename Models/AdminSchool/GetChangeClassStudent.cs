namespace SchoolSystem.Models.AdminSchool
{
    public class GetChangeClassStudent
    {
        public Guid? idStudent { get; set; }
        public string? nameStudent { get; set; }
        public Guid? lastIdClass { get; set; }
        public Guid? lastIdClassEnc { get; set; }
        public List<theClass>? theClasses { get; set; }

    }
    
    public class theClass
    {
        public string? nameClass { get; set; }
        public Guid? idClass { get; set; }
        
    }

}