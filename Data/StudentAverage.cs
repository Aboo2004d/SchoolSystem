using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StudentAverage
{
    public double AverageGrade { get; set; }

    public int IdStudentAvg { get; set; }

    public int? IdStudent { get; set; }

    public int? IdClass { get; set; }

    public virtual TheClass? IdClassNavigation { get; set; }

    public virtual Student? IdStudentNavigation { get; set; }
}
