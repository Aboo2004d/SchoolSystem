using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class StudentTeacher
{
    public int Id { get; set; }

    public int IdStudent { get; set; }

    public int IdTeacher { get; set; }

    [ForeignKey("IdStudent")]
    public virtual Student IdStudentNavigation { get; set; } = null!;
    
    [ForeignKey("IdTeacher")]
    public virtual Teacher IdTeacherNavigation { get; set; } = null!;
}
