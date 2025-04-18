using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class TeacherLectuer
{
    public int Id { get; set; }

    public int IdTeacher { get; set; }

    public int IdLectuer { get; set; }

    [ForeignKey("IdLectuer")]
    public virtual Lectuer IdLectuerNavigation { get; set; } = null!;

    [ForeignKey("IdTeacher")]
    public virtual Teacher IdTeacherNavigation { get; set; } = null!;
}
