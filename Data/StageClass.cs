using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class StageClass
{
    public int Id { get; set; }

    public string Code { get; set; } = null!;

    public int MinClass { get; set; }

    public int MaxClass { get; set; }

    public string NameStage { get; set; } = null!;

    public virtual ICollection<School> Schools { get; set; } = new List<School>();

    public virtual ICollection<TheClass> TheClasses { get; set; } = new List<TheClass>();
}
