using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class TheClass
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? IdSchool { get; set; }

    public bool? IsDeleted { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual ICollection<StudentAverage> StudentAverages { get; set; } = new List<StudentAverage>();

    public virtual ICollection<StudentLectuerTeacher> StudentLectuerTeachers { get; set; } = new List<StudentLectuerTeacher>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TeacherLectuerClass> TeacherLectuerClasses { get; set; } = new List<TeacherLectuerClass>();
}
