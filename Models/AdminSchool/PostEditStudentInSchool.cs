namespace SchoolSystem.Models.AdminSchool
{

    public partial class PostEditStudentInSchool
    {
        public Guid? idStudent { get; set; }

        public string? nameStudent { get; set; }

        public string? phone { get; set; }

        public string? emailAddressStudent { get; set; }

        public DateOnly? theDate { get; set; }

        public Guid? idClass { get; set; }

        public string? city { get; set; }

        public string? area { get; set; }

        public int? idNumber { get; set; }

    }
}