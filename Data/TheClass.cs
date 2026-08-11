using System;
using System.Collections.Generic;

namespace SchoolSystem.Data;

public partial class TheClass
{
    public Guid Id { get; set; }

    public string Name { get; set; } = null!;

    public Guid? IdSchool { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsDeletedSchool { get; set; }

    public Guid? IdStage { get; set; }

    public int? NumberClass { get; set; }

    public int? Section { get; set; }

    public Guid? IdBranch { get; set; }

    public virtual ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();

    public virtual ICollection<Grade> Grades { get; set; } = new List<Grade>();

    public virtual Branch? IdBranchNavigation { get; set; }

    public virtual School? IdSchoolNavigation { get; set; }

    public virtual StageClass? IdStageNavigation { get; set; }

    public virtual ICollection<StudentLectuerTeacher> StudentLectuerTeachers { get; set; } = new List<StudentLectuerTeacher>();

    public virtual ICollection<Student> Students { get; set; } = new List<Student>();

    public virtual ICollection<TeacherLectuerClass> TeacherLectuerClasses { get; set; } = new List<TeacherLectuerClass>();
}
