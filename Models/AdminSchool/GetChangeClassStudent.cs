namespace SchoolSystem.Models.AdminSchool
{
    public class GetChangeClassStudent
    {
        public string? idStudent { get; set; }
        public string? nameStudent { get; set; }
        public int? lastIdClass { get; set; }
        public string? lastIdClassEnc { get; set; }
        public List<theClass>? theClasses { get; set; }

    }
    
    public class theClass
    {
        public string? nameClass { get; set; }
        public int? idClass { get; set; }
        
    }

}