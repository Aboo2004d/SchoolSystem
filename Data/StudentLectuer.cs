using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class StudentLectuer
{
    public int Id { get; set; }

    [Required]
    [Display(Name="Id Student")]
    public int? IdStudent { get; set; }

    [Required]
    [Display(Name="Id Lectuer")]
    public int? IdLectuer { get; set; }

    [Display(Name="Lectuer")]
    [ForeignKey("IdLectuer")]
    public virtual Lectuer? IdLectuerNavigation { get; set; }

    [Display(Name="Student")]
    [ForeignKey("IdStudent")]
    public virtual Student? IdStudentNavigation { get; set; }
}
