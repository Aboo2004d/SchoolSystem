using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StudentLectuer
{
    public int Id { get; set; }

    public int IdStudent { get; set; }

    public int IdLectuer { get; set; }

    public virtual Lectuer IdLectuerNavigation { get; set; } = null!;

    public virtual Student IdStudentNavigation { get; set; } = null!;
}
