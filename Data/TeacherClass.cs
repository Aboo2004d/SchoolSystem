using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class TeacherClass
{
    public int Id { get; set; }

    public int IdTeacher { get; set; }

    public int IdClass { get; set; }

    public virtual TheClass IdClassNavigation { get; set; } = null!;

    public virtual Teacher IdTeacherNavigation { get; set; } = null!;
}
