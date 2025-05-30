using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Gender
{
    public int Id { get; set; }

    public string TheType { get; set; } = null!;

    public virtual ICollection<School> Schools { get; set; } = new List<School>();
}
