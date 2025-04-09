using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class StudentTeacher
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Id Student")]
    public int? IdStudent { get; set; }

    [Required]
    [Display(Name = "Id Teacher")]
    public int? IdTeacher { get; set; }

    [Display(Name = "Student")]
    [ForeignKey("IdStudent")]
    public virtual Student? IdStudentNavigation { get; set; }

    [Display(Name = "Teacher")]
    [ForeignKey("IdTeacher")]
    public virtual Teacher? IdTeacherNavigation { get; set; }
}
