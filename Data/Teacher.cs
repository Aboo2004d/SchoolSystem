using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Teacher
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int Phone { get; set; }

    public string? Email { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual ICollection<StudentTeacher> StudentTeachers { get; set; } = new List<StudentTeacher>();

    public virtual ICollection<TeacherClass> TeacherClasses { get; set; } = new List<TeacherClass>();

    public virtual ICollection<TeacherLectuer> TeacherLectuers { get; set; } = new List<TeacherLectuer>();
}
