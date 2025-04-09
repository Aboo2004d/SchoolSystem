using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Data;

public partial class Teacher
{
    public int Id { get; set; }

    [Display(Name="Full Name")]
    public string Name { get; set; } = null!;

    [Display(Name="Phone Number")]
    public int Phone { get; set; }

    [Display(Name="Email Address")]
    [EmailAddress(ErrorMessage = "Email Address is not valid")]
    public string? Email { get; set; }

    public virtual ICollection<StudentTeacher> StudentTeachers { get; set; } = new List<StudentTeacher>();

    public virtual ICollection<TeacherClass> TeacherClasses { get; set; } = new List<TeacherClass>();

    public virtual ICollection<TeacherLectuer> TeacherLectuers { get; set; } = new List<TeacherLectuer>();
}
