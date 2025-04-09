using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SchoolSystem.Data;

public partial class TheClass
{
    public int Id { get; set; }

    [Display(Name = "Name of Class")]
    public string Name { get; set; } = null!;

    public virtual ICollection<ClassLectuer> ClassLectuers { get; set; } = new List<ClassLectuer>();

    public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

    public virtual ICollection<TeacherClass> TeacherClasses { get; set; } = new List<TeacherClass>();
}
