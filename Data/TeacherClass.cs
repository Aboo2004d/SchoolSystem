using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class TeacherClass
{
    public int Id { get; set; }

    public int IdTeacher { get; set; }

    public int IdClass { get; set; }

    [ForeignKey("IdClass")]
    public virtual TheClass IdClassNavigation { get; set; } = null!;
    [ForeignKey("IdTeacher")]
    public virtual Teacher IdTeacherNavigation { get; set; } = null!;
}
