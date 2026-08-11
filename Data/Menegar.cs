using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Menegar
{
    public Guid? ApplicationUserId { get; set; }
    public virtual ApplicationUser? ApplicationUser { get; set; }

    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int? IdSchool { get; set; }

    public DateOnly? TheDate { get; set; }

    public string? City { get; set; }

    public string? Area { get; set; }

    public int? IdNumber { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsDeletedSchool { get; set; }

    public virtual School? IdSchoolNavigation { get; set; }
}
