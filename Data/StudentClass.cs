using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StudentClass
{
    public Guid Id { get; set; }

    public Guid? IdStudent { get; set; }

    public Guid? IdClass { get; set; }

    public Guid? IdSchool { get; set; }

    public Guid? IdTeacher { get; set; }

    public Guid? IdLectuer { get; set; }

    public virtual TheClass? IdClassNavigation { get; set; }

    public virtual Lectuer? IdLectuerNavigation { get; set; }

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual Student? IdStudentNavigation { get; set; }

    public virtual Teacher? IdTeacherNavigation { get; set; }
}
