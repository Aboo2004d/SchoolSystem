using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class School
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public int? IdStatusSchool { get; set; }

    public int? IdGender { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();


    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual Gender? IdGenderNavigation { get; set; }

    public virtual StatusSchool? IdStatusSchoolNavigation { get; set; }

    public virtual ICollection<Lectuer> Lectuers { get; set; } = new List<Lectuer>();

    public virtual ICollection<Menegar> Menegars { get; set; } = new List<Menegar>();


    public virtual ICollection<StudentLectuerTeacher> StudentLectuerTeachers { get; set; } = new List<StudentLectuerTeacher>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TeacherLectuerClass> TeacherLectuerClasses { get; set; } = new List<TeacherLectuerClass>();

    public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();

    public virtual ICollection<TheClass> TheClasses { get; set; } = new List<TheClass>();
}
