using System;
using System.Collections.Generic;


public partial class AttendanceViewModel
{
    public int Id { get; set; }

    public string AttendanceStatus { get; set; } = null!;

    public DateOnly? DateAndTime { get; set; }

    public string? Excuse { get; set; }
    public string? StudentName { get; set; }
    public string? ClassroomName { get; set; }
    public string? LectuerName { get; set; }


}
