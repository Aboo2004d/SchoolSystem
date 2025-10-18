namespace SchoolSystem.Models.AdminSchool
{

    public partial class GetEditTeacherInSchool
    {
        public string? idTeacher { get; set; }

        public string? nameTeacher { get; set; }

        public string? phone { get; set; }

        public string? emailAddressTeacher { get; set; }

        public string? nameSchool { get; set; }

        public DateOnly? theDate { get; set; }

        public string? city { get; set; }
        public string? area { get; set; }

        public int? idNumber { get; set; }

    }
}