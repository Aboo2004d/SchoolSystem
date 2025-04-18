using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class TheClass
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<ClassLectuer> ClassLectuers { get; set; } = new List<ClassLectuer>();

    public virtual ICollection<StudentAverage> StudentAverages { get; set; } = new List<StudentAverage>();

    public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

    public virtual ICollection<TeacherClass> TeacherClasses { get; set; } = new List<TeacherClass>();
}
