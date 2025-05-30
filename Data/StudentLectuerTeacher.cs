using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StudentLectuerTeacher
{
    public int Id { get; set; }

    public int? IdStudent { get; set; }

    public int? IdLectuer { get; set; }

    public int? IdSchool { get; set; }

    public int? IdClass { get; set; }

    public int? IdTeacher { get; set; }

    public virtual TheClass? IdClassNavigation { get; set; }

    public virtual Lectuer? IdLectuerNavigation { get; set; }

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual Student? IdStudentNavigation { get; set; }

    public virtual Teacher? IdTeacherNavigation { get; set; }
}
