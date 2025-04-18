using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class StudentClass
{
    public int Id { get; set; }

    public int IdStudent { get; set; }

    public int IdClass { get; set; }

    [ForeignKey("IdClass")]
    public virtual TheClass IdClassNavigation { get; set; } = null!;

    [ForeignKey("IdStudent")]
    public virtual Student IdStudentNavigation { get; set; } = null!;
}
