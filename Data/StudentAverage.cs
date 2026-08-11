using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StudentAverage
{
    public double AverageGrade { get; set; }

    public Guid IdStudentAvg { get; set; }

    public Guid? IdStudent { get; set; }

    public Guid? IdClass { get; set; }

    public Guid? IdSchool { get; set; }

    public bool IsDeletedSchool { get; set; }

    public virtual TheClass? IdClassNavigation { get; set; }

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual Student? IdStudentNavigation { get; set; }
}
