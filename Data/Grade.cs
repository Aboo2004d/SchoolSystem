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

    public int? IdStudent { get; set; }

    public int? IdTeacher { get; set; }

    public int? IdLectuer { get; set; }

    public int? IdClass { get; set; }

    public int? Total { get; set; }

    public int? IdSchool { get; set; }

    public bool IsDeletedGrades { get; set; }

    public bool IsDeletedClass { get; set; }

    public bool IsDeletedLectuer { get; set; }

    public bool IsDeletedStudent { get; set; }

    public bool IsDeletedTeacher { get; set; }

    public bool IsDeletedSchool { get; set; }

    public bool IsTeacherRemovedFromClass { get; set; }

    public bool IsTeacherRemovedFromLectuer { get; set; }

    public virtual TheClass? IdClassNavigation { get; set; }

    public virtual Lectuer? IdLectuerNavigation { get; set; }

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual Student? IdStudentNavigation { get; set; }

    public virtual Teacher? IdTeacherNavigation { get; set; }
}
