namespace SchoolSystem.Models.AdminSchool
{

    public partial class DetailsStudentInSchool
    {
        public Guid? idStudent { get; set; }

        public string? nameStudent { get; set; }

        public string? phoneStudent { get; set; }

        public string? emailAddressStudent { get; set; }

        public string? nameSchool { get; set; }

        public DateOnly? theDate { get; set; }

        public string? nameClass { get; set; }

        public string? address { get; set; }


        public int? idNumber { get; set; }

        public string? isDeleted { get; set; }
    }
}