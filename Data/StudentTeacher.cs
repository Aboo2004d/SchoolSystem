using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StudentTeacher
{
    public int Id { get; set; }

    public int IdStudent { get; set; }

    public int IdTeacher { get; set; }

    public virtual Student IdStudentNavigation { get; set; } = null!;

    public virtual Teacher IdTeacherNavigation { get; set; } = null!;
}
