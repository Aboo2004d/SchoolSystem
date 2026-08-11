namespace SchoolSystem.Models.AdminSchool
{

    public partial class DetailsTeacherInSchool
    {
        public Guid? idTeacher { get; set; }

        public string? nameTeacher { get; set; }

        public string? phone { get; set; }

        public string? emailAddressTeacher { get; set; }

        public string? nameSchool { get; set; }

        public DateOnly? theDate { get; set; }

        public string? address { get; set; }

        public int? idNumber { get; set; }

    }
}