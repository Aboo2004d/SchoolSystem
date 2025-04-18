using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class StudentLectuer
{
    public int Id { get; set; }

    public int IdStudent { get; set; }

    public int IdLectuer { get; set; }

    [ForeignKey("IdLectuer")]
    public virtual Lectuer IdLectuerNavigation { get; set; } = null!;
    [ForeignKey("IdStudent")]
    public virtual Student IdStudentNavigation { get; set; } = null!;
}
