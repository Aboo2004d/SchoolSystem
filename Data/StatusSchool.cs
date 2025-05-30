using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StatusSchool
{
    public int Id { get; set; }

    public bool? Condition { get; set; }

    public string? TheType { get; set; }

    public virtual ICollection<School> Schools { get; set; } = new List<School>();
}
