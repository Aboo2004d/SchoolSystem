using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class TeacherLectuerClass
{
    public Guid Id { get; set; }

    public Guid? IdTeacher { get; set; }

    public Guid? IdLectuer { get; set; }

    public Guid? IdSchool { get; set; }

    public Guid? IdClass { get; set; }

    public bool IsDeletedTeacherLectuerClass { get; set; }

    public bool IsDeletedClass { get; set; }

    public bool IsDeletedLectuer { get; set; }

    public bool IsDeletedTeacher { get; set; }

    public bool IsDeletedSchool { get; set; }

    public bool IsTeacherRemovedFromClass { get; set; }

    public bool IsTeacherRemovedFromLectuer { get; set; }

    public virtual TheClass? IdClassNavigation { get; set; }

    public virtual Lectuer? IdLectuerNavigation { get; set; }

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual Teacher? IdTeacherNavigation { get; set; }
}
