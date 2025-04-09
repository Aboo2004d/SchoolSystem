using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class StudentClass
{
    public int Id { get; set; }

    [Required]
    [Display(Name="Student")]
    public int? IdStudent { get; set; }

    [Required]
    [Display(Name="Class")]
    public int? IdClass { get; set; }

    [Display(Name="Class")]
    [ForeignKey("IdClass")]
    public virtual TheClass? IdClassNavigation { get; set; }
    [Display(Name="Student")]
    [ForeignKey("IdStudent")]
    public virtual Student ? IdStudentNavigation { get; set; } 
}
