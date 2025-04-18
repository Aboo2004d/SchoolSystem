using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class Attendance
{
    public int AttendanceId { get; set; }

    public int PresentDays { get; set; }

    public int AbsentDays { get; set; }

    public int? TotalDays { get; set; }

    public int IdStudent { get; set; }

    [ForeignKey("IdStudent")]
    public virtual Student? IdStudentNavigation { get; set; }
}
