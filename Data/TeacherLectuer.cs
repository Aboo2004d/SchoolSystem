using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class TeacherLectuer
{
    public int Id { get; set; }

    public int IdTeacher { get; set; }

    public int IdLectuer { get; set; }

    public virtual Lectuer IdLectuerNavigation { get; set; } = null!;

    public virtual Teacher IdTeacherNavigation { get; set; } = null!;
}
