using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace SchoolSystem.Data;

public partial class SystemSchoolDbContext : DbContext
{
    public SystemSchoolDbContext(DbContextOptions<SystemSchoolDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Acount> Acounts { get; set; }

    public virtual DbSet<Attendance> Attendances { get; set; }

    public virtual DbSet<ErrorLog> ErrorLogs { get; set; }

    public virtual DbSet<Gender> Genders { get; set; }

    public virtual DbSet<Grade> Grades { get; set; }

    public virtual DbSet<Lectuer> Lectuers { get; set; }

    public virtual DbSet<Menegar> Menegars { get; set; }

    public virtual DbSet<ProfileImage> ProfileImages { get; set; }

    public virtual DbSet<School> Schools { get; set; }

    public virtual DbSet<StatusSchool> StatusSchools { get; set; }

    public virtual DbSet<Student> Students { get; set; }

    public virtual DbSet<StudentAverage> StudentAverages { get; set; }

    public virtual DbSet<StudentLectuerTeacher> StudentLectuerTeachers { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherLectuerClass> TeacherLectuerClasses { get; set; }

    public virtual DbSet<TheClass> TheClasses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Acount>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Acounts__3213E83F1B5AE3B5");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email).HasMaxLength(50);
            entity.Property(e => e.Passwords).HasMaxLength(500);
            entity.Property(e => e.ResetToken).HasMaxLength(200);
            entity.Property(e => e.ResetTokenExpiry).HasColumnType("datetime");
            entity.Property(e => e.Role).HasMaxLength(50);
            entity.Property(e => e.UsersName)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Attendance>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Attendan__3213E83FAD8350D2");

            entity.ToTable("Attendance");

            entity.HasIndex(e => e.IdSchool, "IX_Attendance_IdSchool");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AttendanceStatus)
                .HasMaxLength(1)
                .IsUnicode(false)
                .HasDefaultValue("0")
                .IsFixedLength();
            entity.Property(e => e.Excuse).HasColumnType("text");

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
                .HasMaxLength(50)
                .IsUnicode(false);

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

            entity.HasOne(d => d.IdGenderNavigation).WithMany(p => p.Schools)
                .HasForeignKey(d => d.IdGender)
                .HasConstraintName("FK__School__IdGender__28B808A7");

            entity.HasOne(d => d.IdStatusSchoolNavigation).WithMany(p => p.Schools)
                .HasForeignKey(d => d.IdStatusSchool)
                .HasConstraintName("FK__School__IdStatus__22FF2F51");
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

        modelBuilder.Entity<StudentAverage>(entity =>
        {
            entity.HasKey(e => e.IdStudentAvg).HasName("PK__StudentA__9A002AFC4CA4B91A");

            entity.ToTable("StudentAverage");

            entity.HasIndex(e => e.IdSchool, "IX_StudentAverage_IdSchool");

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.StudentAverages)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK__StudentAv__IdCla__03BB8E22");

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.StudentAverages)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK__StudentAv__IdSch__6E565CE8");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.StudentAverages)
                .HasForeignKey(d => d.IdStudent)
                .HasConstraintName("FK_StudentAverage_Student");
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

            entity.HasOne(d => d.IdSchoolNavigation).WithMany(p => p.TheClasses)
                .HasForeignKey(d => d.IdSchool)
                .HasConstraintName("FK_TheClass_School");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
