using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Student
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Phone { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual ICollection<StudentAverage> StudentAverages { get; set; } = new List<StudentAverage>();

    public virtual ICollection<StudentClass> StudentClasses { get; set; } = new List<StudentClass>();

    public virtual ICollection<StudentLectuer> StudentLectuers { get; set; } = new List<StudentLectuer>();

    public virtual ICollection<StudentTeacher> StudentTeachers { get; set; } = new List<StudentTeacher>();
}
