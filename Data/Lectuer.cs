using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class Lectuer
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;
    [NotMapped]
    public int NumberOfStudentsInLectuer { get; set; }
    
    [NotMapped]
    public int NumberOfTeacherInLectuer { get; set; }
    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<ClassLectuer> ClassLectuers { get; set; } = new List<ClassLectuer>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual ICollection<StudentLectuer> StudentLectuers { get; set; } = new List<StudentLectuer>();

    public virtual ICollection<TeacherLectuer> TeacherLectuers { get; set; } = new List<TeacherLectuer>();
}
