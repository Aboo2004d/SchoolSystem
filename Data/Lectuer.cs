using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSystem.Data;

public partial class Lectuer
{
    public int Id { get; set; }

    [NotMapped]
    public int NumberOfStudentsInLectuer { get; set; }

    [NotMapped]
    public int NumberOfTeacherInLectuer { get; set; }

    public string Name { get; set; } = null!;

    public int? IdSchool { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();


    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual ICollection<StudentLectuerTeacher> StudentLectuerTeachers { get; set; } = new List<StudentLectuerTeacher>();

    

    public virtual ICollection<TeacherLectuerClass> TeacherLectuerClasses { get; set; } = new List<TeacherLectuerClass>();
}
