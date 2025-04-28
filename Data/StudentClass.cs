using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StudentClass
{
    public int Id { get; set; }

    public int IdStudent { get; set; }

    public int IdClass { get; set; }

    public virtual TheClass IdClassNavigation { get; set; } = null!;

    public virtual Student IdStudentNavigation { get; set; } = null!;
}
