using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class Teacher 
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public int? IdSchool { get; set; }

    public DateOnly? TheDate { get; set; }

    public string? City { get; set; }

    public string? Area { get; set; }

    public int? IdNumber { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual ICollection<StudentLectuerTeacher> StudentLectuerTeachers { get; set; } = new List<StudentLectuerTeacher>();

    public virtual ICollection<TeacherLectuerClass> TeacherLectuerClasses { get; set; } = new List<TeacherLectuerClass>();
}
