using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Grade
{
    public int GradesId { get; set; }

    public int? FirstMonth { get; set; }

    public int? Mid { get; set; }

    public int? SecondMonth { get; set; }

    public int? Activity { get; set; }

    public int? Final { get; set; }

    public int? Total { get; set; }

    public int? IdStudent { get; set; }

    public int? IdTeacher { get; set; }

    public int? IdLectuer { get; set; }

    public virtual Lectuer? IdLectuerNavigation { get; set; }

    public virtual Student? IdStudentNavigation { get; set; }

    public virtual Teacher? IdTeacherNavigation { get; set; }
}
