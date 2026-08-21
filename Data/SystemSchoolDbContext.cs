using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace SchoolSystem.Data;

public partial class SystemSchoolDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public SystemSchoolDbContext(DbContextOptions<SystemSchoolDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<Branch> Branches { get; set; }

    public virtual DbSet<ErrorLog> ErrorLogs { get; set; }

    public virtual DbSet<Gender> Genders { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Lectuer> Lectuers { get; set; }

    public virtual DbSet<Menegar> Menegars { get; set; }

    public virtual DbSet<ProfileImage> ProfileImages { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<StageClass> StageClasses { get; set; }

    public virtual DbSet<StatusSchool> StatusSchools { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentLectuerTeacher> StudentLectuerTeachers { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherLectuerClass> TeacherLectuerClasses { get; set; }

    public virtual DbSet<TheClass> TheClasses { get; set; }

    public virtual DbSet<Directorate> Directorates { get; set; }

    public virtual DbSet<DirectorateManager> DirectorateManagers { get; set; }

    public virtual DbSet<Ministry> Ministries { get; set; }

    public virtual DbSet<MinistryManager> MinistryManagers { get; set; }

    public virtual DbSet<TeacherPlacement> TeacherPlacements { get; set; }

    public virtual DbSet<SchoolManagerAssignment> SchoolManagerAssignments { get; set; }

    public virtual DbSet<StudentEnrollment> StudentEnrollments { get; set; }

    public virtual DbSet<TransferRequest> TransferRequests { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Ministry>(entity =>
        {
            entity.ToTable("Ministry");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<MinistryManager>(entity =>
        {
            entity.ToTable("MinistryManager");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.MinistryId).IsUnique();
            entity.HasIndex(x => x.ApplicationUserId).IsUnique().HasFilter("[ApplicationUserId] IS NOT NULL");
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.HasOne(x => x.Ministry).WithOne(x => x.Manager)
                .HasForeignKey<MinistryManager>(x => x.MinistryId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ApplicationUser).WithOne(x => x.MinistryManager)
                .HasForeignKey<MinistryManager>(x => x.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<TeacherPlacement>(entity =>
        {
            entity.ToTable("TeacherPlacement");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TeacherId, x.SchoolId }).IsUnique().HasFilter("[IsActive] = 1");
            entity.HasIndex(x => x.TeacherId).IsUnique().HasFilter("[IsActive] = 1 AND [IsPrimary] = 1");
            entity.HasOne(x => x.Teacher).WithMany(x => x.Placements)
                .HasForeignKey(x => x.TeacherId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.School).WithMany(x => x.TeacherPlacements)
                .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<SchoolManagerAssignment>(entity =>
        {
            entity.ToTable("SchoolManagerAssignment");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.ManagerId, x.SchoolId }).IsUnique().HasFilter("[IsActive] = 1");
            entity.HasIndex(x => x.ManagerId).IsUnique().HasFilter("[IsActive] = 1 AND [IsPrimary] = 1");
            entity.HasOne(x => x.Manager).WithMany(x => x.SchoolAssignments)
                .HasForeignKey(x => x.ManagerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.School).WithMany(x => x.ManagerAssignments)
                .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<StudentEnrollment>(entity =>
        {
            entity.ToTable("StudentEnrollment");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.StudentId).IsUnique().HasFilter("[IsActive] = 1");
            entity.HasOne(x => x.Student).WithMany(x => x.Enrollments)
                .HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.School).WithMany(x => x.StudentEnrollments)
                .HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.Class).WithMany()
                .HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TransferRequest>(entity =>
        {
            entity.ToTable("TransferRequest");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SubjectType).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Status).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Reason).HasMaxLength(1000);
            entity.Property(x => x.DecisionNote).HasMaxLength(1000);
            entity.HasIndex(x => new { x.SubjectType, x.SubjectId, x.Status });
            entity.HasIndex(x => new { x.SubjectType, x.SubjectIdentityNumber, x.Status });
            entity.HasIndex(x => x.SourceDirectorateId);
            entity.HasIndex(x => x.DestinationDirectorateId);
            entity.HasIndex(x => x.SourceMinistryId);
            entity.HasIndex(x => x.DestinationMinistryId);
        });
        modelBuilder.Entity<Directorate>(entity =>
        {
            entity.ToTable("Directorate");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Code).IsUnique();
            entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.MinistryId);
            entity.HasOne(x => x.Ministry).WithMany(x => x.Directorates)
                .HasForeignKey(x => x.MinistryId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Area).HasMaxLength(100);
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(256);
        });

        modelBuilder.Entity<DirectorateManager>(entity =>
        {
            entity.ToTable("DirectorateManager");
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.DirectorateId).IsUnique();
            entity.HasIndex(x => x.ApplicationUserId).IsUnique().HasFilter("[ApplicationUserId] IS NOT NULL");
            entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(30);
            entity.Property(x => x.Email).HasMaxLength(256);
            entity.Property(x => x.City).HasMaxLength(100);
            entity.Property(x => x.Area).HasMaxLength(100);
            entity.HasOne(x => x.Directorate).WithOne(x => x.Manager)
                .HasForeignKey<DirectorateManager>(x => x.DirectorateId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.ApplicationUser).WithOne(x => x.DirectorateManager)
                .HasForeignKey<DirectorateManager>(x => x.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3213E83FAD8350D2");

            entity.ToTable("Attendance");

            entity.HasIndex(e => e.IdSchool, "IX_Attendance_IdSchool");

            entity.HasIndex(e => new { e.IdSchool, e.IdTeacher },
                "IX_Attendance_IdSchool_IdTeacher");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttendanceStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("0")
                .IsFixedLength();
            // SQL Server's legacy text type cannot be sorted and has limited comparison support.
            // nvarchar(max) preserves existing content while supporting search and ordering.
            entity.Property(e => e.Excuse).HasColumnType("nvarchar(max)");

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK_Attendance_TheClass");

            entity.HasOne(d => d.IdLectuerNavigation).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.IdLectuer)
                .HasConstraintName("FK_Attendance_Lectuer");

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK_Attendance_School");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.IdStudent)
                .HasConstraintName("FK_Attendance_Student");

            entity.HasOne(d => d.IdTeacherNavigation).WithMany(p => p.Attendances)
                .HasForeignKey(d => d.IdTeacher)
                .HasConstraintName("FK_Attendance_Teacher");
        });

        modelBuilder.Entity<Branch>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Branch__54205B04058BBB4F");

            entity.ToTable("Branch");

            entity.Property(e => e.BranchCode)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.BranchName).HasMaxLength(100);
        });

        modelBuilder.Entity<ErrorLog>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ErrorLog__3214EC07A2529995");

            entity.Property(e => e.LoggedAt)
                .HasDefaultValueSql("(getutcdate())")
                .HasColumnType("datetime");
            entity.Property(e => e.Source).HasMaxLength(255);
        });

        modelBuilder.Entity<Gender>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Gender__3214EC070A22819D");

            entity.ToTable("Gender");

            entity.Property(e => e.TheType).HasMaxLength(7);
        });

        modelBuilder.Entity<Grade>(entity =>
        {
            entity.HasKey(e => e.GradesId).HasName("PK__Grades__931A40BF88D8CDCA");

            entity.HasIndex(e => e.GradesId, "IX_Grades_Id");

            entity.HasIndex(e => e.IdSchool, "IX_Grades_IdSchool");

            entity.Property(e => e.GradesId).HasColumnName("GradesID");
            entity.Property(e => e.Activity).HasDefaultValue(0);
            entity.Property(e => e.Final).HasDefaultValue(0);
            entity.Property(e => e.FirstMonth).HasDefaultValue(0);
            entity.Property(e => e.Mid).HasDefaultValue(0);
            entity.Property(e => e.SecondMonth).HasDefaultValue(0);
            entity.Property(e => e.Total).HasComputedColumnSql("(((([FirstMonth]+[Mid])+[SecondMonth])+[Activity])+[Final])", false);

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.Grades)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK__Grades__IdClass__0D0FEE32");

            entity.HasOne(d => d.IdLectuerNavigation).WithMany(p => p.Grades)
                .HasForeignKey(d => d.IdLectuer)
                .HasConstraintName("FK_Grades_Lectuer");

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.Grades)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK__Grades__IdSchool__6D6238AF");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.Grades)
                .HasForeignKey(d => d.IdStudent)
                .HasConstraintName("FK_Grades_Student");

            entity.HasOne(d => d.IdTeacherNavigation).WithMany(p => p.Grades)
                .HasForeignKey(d => d.IdTeacher)
                .HasConstraintName("FK_Grades_Teacher");
        });

        modelBuilder.Entity<Lectuer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Lectuer__3213E83FBA6843F5");

            entity.ToTable("Lectuer");

            entity.HasIndex(e => e.IdSchool, "IX_Lectuer_IdSchool");

            entity.HasIndex(e => e.Name, "IX_Lectuer_Name");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(100);

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.Lectuers)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK__Lectuer__IdSchoo__6C6E1476");
        });

        modelBuilder.Entity<Menegar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Menegar__3213E83FE96BFA1F");

            entity.ToTable("Menegar");

            entity.HasIndex(e => e.IdSchool, "IX_Menegar_IdSchool");

            entity.HasIndex(e => e.Name, "IX_Menegar_Name");

            entity.HasIndex(e => e.IdNumber, "UQ_Menegar_IdNumber").IsUnique();
            entity.HasIndex(e => e.ApplicationUserId).IsUnique().HasFilter("[ApplicationUserId] IS NOT NULL");
            entity.HasOne(e => e.ApplicationUser).WithOne(e => e.Menegar)
                .HasForeignKey<Menegar>(e => e.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Area).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.Menegars)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK__Menegar__IdSchoo__23F3538A");
        });

        modelBuilder.Entity<ProfileImage>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ProfileI__3214EC07AD185F4E");

            entity.ToTable("ProfileImage");

            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.ProfileImagePath).HasMaxLength(200);
            entity.Property(e => e.UserName).HasMaxLength(100);
        });

        modelBuilder.Entity<School>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__School__3214EC07F11AFDBA");

            entity.ToTable("School");

            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.IsActive).HasDefaultValue(true);
            entity.Property(e => e.OwnershipType).HasMaxLength(30).HasDefaultValue(SchoolOwnershipTypes.Government);
            entity.Property(e => e.EducationLevel).HasMaxLength(30).HasDefaultValue(SchoolEducationLevels.Primary);

            entity.HasIndex(e => e.DirectorateId);
            entity.HasOne(e => e.Directorate).WithMany(e => e.Schools)
                .HasForeignKey(e => e.DirectorateId).OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.IdGenderNavigation).WithMany(p => p.Schools)
                .HasForeignKey(d => d.IdGender)
                .HasConstraintName("FK__School__IdGender__28B808A7");

            entity.HasOne(d => d.IdStageNavigation).WithMany(p => p.Schools)
                .HasForeignKey(d => d.IdStage)
                .HasConstraintName("FK__School__IdStage__377B294A");

            entity.HasOne(d => d.IdStatusSchoolNavigation).WithMany(p => p.Schools)
                .HasForeignKey(d => d.IdStatusSchool)
                .HasConstraintName("FK__School__IdStatus__22FF2F51");
        });

        modelBuilder.Entity<StageClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StageCla__3214EC07B7F14D02");

            entity.ToTable("StageClass");

            entity.HasIndex(e => e.Code, "UQ__StageCla__A25C5AA7AAECF2F1").IsUnique();

            entity.Property(e => e.Code)
                .HasMaxLength(1)
                .IsUnicode(false)
                .IsFixedLength();
            entity.Property(e => e.NameStage).HasMaxLength(15);
        });

        modelBuilder.Entity<StatusSchool>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StatusSc__3214EC073DB2CBA3");

            entity.ToTable("StatusSchool");

            entity.Property(e => e.Condition).HasColumnName("condition");
            entity.Property(e => e.TheType).HasMaxLength(20);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student__3213E83F6F20DDDC");

            entity.ToTable("Student");

            entity.HasIndex(e => e.IdSchool, "IX_Student_IdSchool");

            entity.HasIndex(e => e.Name, "IX_Student_Name");
            entity.HasIndex(e => e.ApplicationUserId).IsUnique().HasFilter("[ApplicationUserId] IS NOT NULL");
            entity.HasOne(e => e.ApplicationUser).WithOne(e => e.Student)
                .HasForeignKey<Student>(e => e.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Area).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.Students)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK__Student__IdClass__1A1FD08D");

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.Students)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK__Student__IdSchoo__24E777C3");
        });

        modelBuilder.Entity<StudentLectuerTeacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentL__3213E83F373E3AFF");

            entity.ToTable("StudentLectuerTeacher");

            entity.HasIndex(e => e.IdSchool, "IX_StudentLectuer_IdSchool");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.StudentLectuerTeachers)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK__StudentLe__IdCla__08F5448B");

            entity.HasOne(d => d.IdLectuerNavigation).WithMany(p => p.StudentLectuerTeachers)
                .HasForeignKey(d => d.IdLectuer)
                .HasConstraintName("FK_StudentLectuer_Lectuer");

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.StudentLectuerTeachers)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK_StudentLectuer_School");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.StudentLectuerTeachers)
                .HasForeignKey(d => d.IdStudent)
                .HasConstraintName("FK_StudentLectuer_Student");

            entity.HasOne(d => d.IdTeacherNavigation).WithMany(p => p.StudentLectuerTeachers)
                .HasForeignKey(d => d.IdTeacher)
                .HasConstraintName("FK__StudentLe__IdTea__09E968C4");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teacher__3213E83F92AB32EF");

            entity.ToTable("Teacher");

            entity.HasIndex(e => e.IdSchool, "IX_Teacher_IdSchool");

            entity.HasIndex(e => e.Name, "IX_Teacher_Name");
            entity.HasIndex(e => e.ApplicationUserId).IsUnique().HasFilter("[ApplicationUserId] IS NOT NULL");
            entity.HasOne(e => e.ApplicationUser).WithOne(e => e.Teacher)
                .HasForeignKey<Teacher>(e => e.ApplicationUserId).OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(e => e.IdNumber, "UQ_Teacher_IdNumber").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Area).HasMaxLength(50);
            entity.Property(e => e.City).HasMaxLength(50);
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.Phone)
                .HasMaxLength(50)
                .IsUnicode(false);

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.Teachers)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK__Teacher__IdSchoo__25DB9BFC");
        });

        modelBuilder.Entity<TeacherLectuerClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TeacherL__3213E83F5B2FD59A");

            entity.ToTable("TeacherLectuerClass");

            entity.HasIndex(e => e.IdSchool, "IX_TeacherLectuer_IdSchool");

            entity.HasIndex(e => new { e.IdSchool, e.IdClass, e.IdLectuer }, "UX_TeacherLectuer_ActiveSchoolClassSubject")
                .IsUnique().HasFilter("[IsDeletedTeacherLectuerClass] = 0 AND [IsDeletedClass] = 0 AND [IsDeletedLectuer] = 0 AND [IsDeletedTeacher] = 0 AND [IsDeletedSchool] = 0");

            entity.Property(e => e.Id).HasColumnName("id");

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.TeacherLectuerClasses)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK__TeacherLe__IdCla__74EE4BDE");

            entity.HasOne(d => d.IdLectuerNavigation).WithMany(p => p.TeacherLectuerClasses)
                .HasForeignKey(d => d.IdLectuer)
                .HasConstraintName("FK_TeacherLectuer_Lectuer");

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.TeacherLectuerClasses)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK__TeacherLe__IdSch__7D2E8C24");

            entity.HasOne(d => d.IdTeacherNavigation).WithMany(p => p.TeacherLectuerClasses)
                .HasForeignKey(d => d.IdTeacher)
                .HasConstraintName("FK_TeacherLectuer_Teacher");
        });

        modelBuilder.Entity<TheClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TheClass__3213E83FD60CD186");

            entity.ToTable("TheClass");

            entity.HasIndex(e => e.IdSchool, "IX_TheClass_IdSchool");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasMaxLength(20);

            entity.HasOne(d => d.IdBranchNavigation).WithMany(p => p.TheClasses)
                .HasForeignKey(d => d.IdBranch)
                .HasConstraintName("FK__TheClass__IdBran__3592E0D8");

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.TheClasses)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK_TheClass_School");

            entity.HasOne(d => d.IdStageNavigation).WithMany(p => p.TheClasses)
                .HasForeignKey(d => d.IdStage)
                .HasConstraintName("FK__TheClass__IdStag__32B6742D");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
