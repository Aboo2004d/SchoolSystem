using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Data;

public partial class Student
{
    public int Id { get; set; }

    [Display(Name="Full Name")]
    public string Name { get; set; } = null!;

    [Display(Name="Phone Number")]
    public int Phone { get; set; }

    [Display(Name="Email Address")]
    [EmailAddress(ErrorMessage = "Email Address is not valid")]
    public string? Email { get; set; }

    public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

    public virtual ICollection<StudentLectuer> StudentLectuers { get; set; } = new List<StudentLectuer>();

    public virtual ICollection<StudentTeacher> StudentTeachers { get; set; } = new List<StudentTeacher>();
}
