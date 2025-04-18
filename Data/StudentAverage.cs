using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class StudentAverage
{
    public double AverageGrade { get; set; }

    public int IdStudentAvg { get; set; }

    public int? IdStudent { get; set; }

    public int? IdClass { get; set; }

    [ForeignKey("IdClass")]
    public virtual TheClass? IdClassNavigation { get; set; }

    [ForeignKey("IdStudent")]
    public virtual Student? IdStudentNavigation { get; set; }
}
