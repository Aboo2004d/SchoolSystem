using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Branch
{
    public Guid Id { get; set; }

    public string BranchName { get; set; } = null!;

    public string BranchCode { get; set; } = null!;

    public virtual ICollection<TheClass> TheClasses { get; set; } = new List<TheClass>();
}
