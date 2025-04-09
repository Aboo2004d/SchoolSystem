using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using SchoolSystem.Models;

namespace SchoolSystem.Data;

public partial class SystemSchoolDbContext : DbContext
{
    public SystemSchoolDbContext(DbContextOptions<SystemSchoolDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ClassLectuer> ClassLectuers { get; set; }

    public virtual DbSet<Lectuer> Lectuers { get; set; }

    public virtual DbSet<Menegar> Menegars { get; set; }

    public virtual DbSet<Student> Students { get; set; }

     public DbSet<Account> Acounts { get; set; }

    public virtual DbSet<StudentClass> StudentClasses { get; set; }

    public virtual DbSet<StudentLectuer> StudentLectuers { get; set; }

    public virtual DbSet<StudentTeacher> StudentTeachers { get; set; }

    public virtual DbSet<Teacher> Teachers { get; set; }

    public virtual DbSet<TeacherClass> TeacherClasses { get; set; }

    public virtual DbSet<TeacherLectuer> TeacherLectuers { get; set; }

    public virtual DbSet<TheClass> TheClasses { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ClassLectuer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__ClassLec__3213E83F8327C66B");

            entity.ToTable("ClassLectuer");

            entity.Property(e => e.Id).HasColumnName("id");
            

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.ClassLectuers)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK__ClassLect__IdCla__66603565");

            entity.HasOne(d => d.IdLectuerNavigation).WithMany(p => p.ClassLectuers)
                .HasForeignKey(d => d.IdLectuer)
                .HasConstraintName("FK__ClassLect__IdLec__6754599E");
        });

        modelBuilder.Entity<Lectuer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Lectuer__3213E83FBA6843F5");

            entity.ToTable("Lectuer");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Menegar>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Menegar__3213E83FE96BFA1F");

            entity.ToTable("Menegar");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<Student>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Student__3213E83F6F20DDDC");

            entity.ToTable("Student");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<StudentClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentC__3213E83F95F82A92");

            entity.ToTable("StudentClass");

            entity.Property(e => e.Id).HasColumnName("id");
            

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.StudentClasses)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK__StudentCl__IdCla__5812160E");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.StudentClasses)
                .HasForeignKey(d => d.IdStudent)
                .HasConstraintName("FK__StudentCl__IdStu__571DF1D5");
        });

        modelBuilder.Entity<StudentLectuer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentL__3213E83F373E3AFF");

            entity.ToTable("StudentLectuer");

            entity.Property(e => e.Id).HasColumnName("id");
           

            entity.HasOne(d => d.IdLectuerNavigation).WithMany(p => p.StudentLectuers)
                .HasForeignKey(d => d.IdLectuer)
                .HasConstraintName("FK__StudentLe__IdLec__5BE2A6F2");

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.StudentLectuers)
                .HasForeignKey(d => d.IdStudent)
                .HasConstraintName("FK__StudentLe__IdStu__5AEE82B9");
        });

        modelBuilder.Entity<StudentTeacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__StudentT__3213E83F7AD0C21E");

            entity.ToTable("StudentTeacher");

            entity.Property(e => e.Id).HasColumnName("id");
           

            entity.HasOne(d => d.IdStudentNavigation).WithMany(p => p.StudentTeachers)
                .HasForeignKey(d => d.IdStudent)
                .HasConstraintName("FK__StudentTe__IdStu__534D60F1");

            entity.HasOne(d => d.IdTeacherNavigation).WithMany(p => p.StudentTeachers)
                .HasForeignKey(d => d.IdTeacher)
                .HasConstraintName("FK__StudentTe__IdTea__5441852A");
        });

        modelBuilder.Entity<Teacher>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__Teacher__3213E83F92AB32EF");

            entity.ToTable("Teacher");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Email)
                .HasMaxLength(50)
                .IsUnicode(false);
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        modelBuilder.Entity<TeacherClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TeacherC__3213E83FDA6F83CA");

            entity.ToTable("TeacherClass");

            entity.Property(e => e.Id).HasColumnName("id");
            

            entity.HasOne(d => d.IdClassNavigation).WithMany(p => p.TeacherClasses)
                .HasForeignKey(d => d.IdClass)
                .HasConstraintName("FK__TeacherCl__IdCla__5FB337D6");

            entity.HasOne(d => d.IdTeacherNavigation).WithMany(p => p.TeacherClasses)
                .HasForeignKey(d => d.IdTeacher)
                .HasConstraintName("FK__TeacherCl__IdTea__5EBF139D");
        });

        modelBuilder.Entity<TeacherLectuer>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TeacherL__3213E83F5B2FD59A");

            entity.ToTable("TeacherLectuer");

            entity.Property(e => e.Id).HasColumnName("id");
            

            entity.HasOne(d => d.IdLectuerNavigation).WithMany(p => p.TeacherLectuers)
                .HasForeignKey(d => d.IdLectuer)
                .HasConstraintName("FK__TeacherLe__IdLec__6383C8BA");

            entity.HasOne(d => d.IdTeacherNavigation).WithMany(p => p.TeacherLectuers)
                .HasForeignKey(d => d.IdTeacher)
                .HasConstraintName("FK__TeacherLe__IdTea__628FA481");
        });

        modelBuilder.Entity<TheClass>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PK__TheClass__3213E83FD60CD186");

            entity.ToTable("TheClass");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsUnicode(false);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
