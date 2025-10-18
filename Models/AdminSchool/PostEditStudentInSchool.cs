namespace SchoolSystem.Models.AdminSchool
{

    public partial class PostEditStudentInSchool
    {
        public string? idStudent { get; set; }

        public string? nameStudent { get; set; }

        public string? phone { get; set; }

        public string? emailAddressStudent { get; set; }

        public DateOnly? theDate { get; set; }

        public int? idClass { get; set; }

        public string? city { get; set; }

        public string? area { get; set; }

        public int? idNumber { get; set; }

    }
}