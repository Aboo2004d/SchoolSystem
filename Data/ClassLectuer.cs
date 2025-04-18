using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class ClassLectuer
{
    public int Id { get; set; }

    public int IdClass { get; set; }

    public int IdLectuer { get; set; }
    [ForeignKey("IdClass")]
    public virtual TheClass IdClassNavigation { get; set; } = null!;

    [ForeignKey("IdLectuer")]
    public virtual Lectuer IdLectuerNavigation { get; set; } = null!;
}
