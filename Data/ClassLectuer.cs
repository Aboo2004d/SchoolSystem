using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class ClassLectuer
{
    public int Id { get; set; }

    public int IdClass { get; set; }

    public int IdLectuer { get; set; }

    public virtual TheClass IdClassNavigation { get; set; } = null!;

    public virtual Lectuer IdLectuerNavigation { get; set; } = null!;
}
