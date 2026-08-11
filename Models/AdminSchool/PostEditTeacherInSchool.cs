namespace SchoolSystem.Models.AdminSchool
{

    public partial class PostEditTeacherInSchool
    {
        public Guid? idTeacher { get; set; }

        public string? nameTeacher { get; set; }

        public string? phone { get; set; }

        public string? emailAddressTeacher { get; set; }


        public DateOnly? theDate { get; set; }

        public string? city { get; set; }
        public string? area { get; set; }

        public int? idNumber { get; set; }

    }
}