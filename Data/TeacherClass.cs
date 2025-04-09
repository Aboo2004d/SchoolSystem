using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class TeacherClass
{
    public int Id { get; set; }

    [Required]
    [Display(Name="Id Teacher")]
    public int? IdTeacher { get; set; }

    [Required]
    [Display(Name="Id Class")]
    public int? IdClass { get; set; }

    [Display(Name="Class")]
    [ForeignKey("IdClass")]
    public virtual TheClass? IdClassNavigation { get; set; }

    [Display(Name="Teacher")]
    [ForeignKey("IdTeacher")]
    public virtual Teacher? IdTeacherNavigation { get; set; }
}
