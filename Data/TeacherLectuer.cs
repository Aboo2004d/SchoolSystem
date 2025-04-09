using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class TeacherLectuer
{
    public int Id { get; set; }

    [Required]
    [Display(Name = "Id Teacher")]
    public int? IdTeacher { get; set; }

    [Required]
    [Display(Name = "Id Lectuer")]
    public int? IdLectuer { get; set; }

    [Display(Name = "Lectuer")]
    [ForeignKey("IdLectuer")]
    public virtual Lectuer? IdLectuerNavigation { get; set; }

    [Display(Name = "Teacher")]
    [ForeignKey("IdTeacher")]
    public virtual Teacher? IdTeacherNavigation { get; set; }
}
