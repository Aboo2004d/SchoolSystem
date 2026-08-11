namespace SchoolSystem.Models.AdminSchool
{

    public partial class GetEditStudentInSchool
    {
        public Guid? idStudent { get; set; }

        public string? nameStudent { get; set; }

        public string? phone { get; set; }

        public string? emailAddressStudent { get; set; }

        public string? nameSchool { get; set; }

        public DateOnly? theDate { get; set; }

        public string? nameClass { get; set; }

        public string? city { get; set; }

        public string? area { get; set; }

        public int? idNumber { get; set; }
        public Guid? idClass { get; set; }

        public string? isDeleted { get; set; }
    }
}