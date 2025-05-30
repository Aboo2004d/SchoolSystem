using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddErrorLogsTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__Attendanc__IdStu__09746778",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK__ClassLect__IdCla__66603565",
                table: "ClassLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK__ClassLect__IdLec__6754599E",
                table: "ClassLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK__Grades__IdLectue__7EF6D905",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK__Grades__IdStuden__7D0E9093",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK__Grades__IdTeache__7E02B4CC",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK__StudentCl__IdCla__5812160E",
                table: "StudentClass");

            migrationBuilder.DropForeignKey(
                name: "FK__StudentCl__IdStu__571DF1D5",
                table: "StudentClass");

            migrationBuilder.DropForeignKey(
                name: "FK__StudentLe__IdLec__5BE2A6F2",
                table: "StudentLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK__StudentLe__IdStu__5AEE82B9",
                table: "StudentLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK__StudentTe__IdStu__534D60F1",
                table: "StudentTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK__StudentTe__IdTea__5441852A",
                table: "StudentTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK__TeacherCl__IdCla__5FB337D6",
                table: "TeacherClass");

            migrationBuilder.DropForeignKey(
                name: "FK__TeacherCl__IdTea__5EBF139D",
                table: "TeacherClass");

            migrationBuilder.DropForeignKey(
                name: "FK__TeacherLe__IdLec__6383C8BA",
                table: "TeacherLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK__TeacherLe__IdTea__628FA481",
                table: "TeacherLectuer");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Attendan__8B69263C0DDD08A8",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "TotalDays",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "AbsentDays",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "PresentDays",
                table: "Attendance");

            migrationBuilder.RenameColumn(
                name: "AttendanceID",
                table: "Attendance",
                newName: "id");

            migrationBuilder.AlterColumn<int>(
                name: "IdTeacher",
                table: "TeacherLectuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdLectuer",
                table: "TeacherLectuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdTeacher",
                table: "TeacherClass",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdClass",
                table: "TeacherClass",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "IdSchool",
                table: "Teacher",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdTeacher",
                table: "StudentTeacher",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdStudent",
                table: "StudentTeacher",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdStudent",
                table: "StudentLectuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdLectuer",
                table: "StudentLectuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdStudent",
                table: "StudentClass",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdClass",
                table: "StudentClass",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "IdSchool",
                table: "Student",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdSchool",
                table: "Menegar",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SecondMonth",
                table: "Grades",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Mid",
                table: "Grades",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "FirstMonth",
                table: "Grades",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Final",
                table: "Grades",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Activity",
                table: "Grades",
                type: "int",
                nullable: true,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdClass",
                table: "Grades",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdLectuer",
                table: "ClassLectuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdClass",
                table: "ClassLectuer",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "IdStudent",
                table: "Attendance",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AttendanceStatus",
                table: "Attendance",
                type: "char(1)",
                unicode: false,
                fixedLength: true,
                maxLength: 1,
                nullable: false,
                defaultValue: "0");

            migrationBuilder.AddColumn<DateOnly>(
                name: "DateAndTime",
                table: "Attendance",
                type: "date",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Excuse",
                table: "Attendance",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdClass",
                table: "Attendance",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdLectuer",
                table: "Attendance",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "IdTeacher",
                table: "Attendance",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "Total",
                table: "Grades",
                type: "int",
                nullable: true,
                computedColumnSql: "(((([FirstMonth]+[Mid])+[SecondMonth])+[Activity])+[Final])",
                stored: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComputedColumnSql: "(((([FirstMonth]+[Mid])+[SecondMonth])+[Activity])+[Final])",
                oldStored: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK__Attendan__3213E83FAD8350D2",
                table: "Attendance",
                column: "id");

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DateOccurred = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gender",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TheType = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Gender__3214EC070A22819D", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LogProgress",
                columns: table => new
                {
                    Step = table.Column<int>(type: "int", nullable: true),
                    Message = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LoggedAt = table.Column<DateTime>(type: "datetime", nullable: true, defaultValueSql: "(getdate())")
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "StatusSchool",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    condition = table.Column<int>(type: "int", nullable: true),
                    TheType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StatusSc__3214EC073DB2CBA3", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "School",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdStatusSchool = table.Column<int>(type: "int", nullable: true),
                    IdGender = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__School__3214EC07F11AFDBA", x => x.Id);
                    table.ForeignKey(
                        name: "FK__School__IdGender__28B808A7",
                        column: x => x.IdGender,
                        principalTable: "Gender",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__School__IdStatus__22FF2F51",
                        column: x => x.IdStatusSchool,
                        principalTable: "StatusSchool",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_IdSchool",
                table: "Teacher",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Student_IdSchool",
                table: "Student",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Menegar_IdSchool",
                table: "Menegar",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdClass",
                table: "Grades",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdClass",
                table: "Attendance",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdLectuer",
                table: "Attendance",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdTeacher",
                table: "Attendance",
                column: "IdTeacher");

            migrationBuilder.CreateIndex(
                name: "IX_School_IdGender",
                table: "School",
                column: "IdGender");

            migrationBuilder.CreateIndex(
                name: "IX_School_IdStatusSchool",
                table: "School",
                column: "IdStatusSchool");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Class",
                table: "Attendance",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Lectuer",
                table: "Attendance",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Student",
                table: "Attendance",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Attendance_Teacher",
                table: "Attendance",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassLectuer_Class",
                table: "ClassLectuer",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_ClassLectuer_Lectuer",
                table: "ClassLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Lectuer",
                table: "Grades",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Student",
                table: "Grades",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Teacher",
                table: "Grades",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__Grades__IdClass__0D0FEE32",
                table: "Grades",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__Menegar__IdSchoo__23F3538A",
                table: "Menegar",
                column: "IdSchool",
                principalTable: "School",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK__Student__IdSchoo__24E777C3",
                table: "Student",
                column: "IdSchool",
                principalTable: "School",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentClass_Class",
                table: "StudentClass",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentClass_Student",
                table: "StudentClass",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentLectuer_Lectuer",
                table: "StudentLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentLectuer_Student",
                table: "StudentLectuer",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentTeacher_Student",
                table: "StudentTeacher",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_StudentTeacher_Teacher",
                table: "StudentTeacher",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__Teacher__IdSchoo__25DB9BFC",
                table: "Teacher",
                column: "IdSchool",
                principalTable: "School",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherClass_Class",
                table: "TeacherClass",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherClass_Teacher",
                table: "TeacherClass",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherLectuer_Lectuer",
                table: "TeacherLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_TeacherLectuer_Teacher",
                table: "TeacherLectuer",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Class",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Lectuer",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Student",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_Attendance_Teacher",
                table: "Attendance");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassLectuer_Class",
                table: "ClassLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK_ClassLectuer_Lectuer",
                table: "ClassLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Lectuer",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Student",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Teacher",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK__Grades__IdClass__0D0FEE32",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK__Menegar__IdSchoo__23F3538A",
                table: "Menegar");

            migrationBuilder.DropForeignKey(
                name: "FK__Student__IdSchoo__24E777C3",
                table: "Student");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentClass_Class",
                table: "StudentClass");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentClass_Student",
                table: "StudentClass");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentLectuer_Lectuer",
                table: "StudentLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentLectuer_Student",
                table: "StudentLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentTeacher_Student",
                table: "StudentTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK_StudentTeacher_Teacher",
                table: "StudentTeacher");

            migrationBuilder.DropForeignKey(
                name: "FK__Teacher__IdSchoo__25DB9BFC",
                table: "Teacher");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherClass_Class",
                table: "TeacherClass");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherClass_Teacher",
                table: "TeacherClass");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherLectuer_Lectuer",
                table: "TeacherLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK_TeacherLectuer_Teacher",
                table: "TeacherLectuer");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "LogProgress");

            migrationBuilder.DropTable(
                name: "School");

            migrationBuilder.DropTable(
                name: "Gender");

            migrationBuilder.DropTable(
                name: "StatusSchool");

            migrationBuilder.DropIndex(
                name: "IX_Teacher_IdSchool",
                table: "Teacher");

            migrationBuilder.DropIndex(
                name: "IX_Student_IdSchool",
                table: "Student");

            migrationBuilder.DropIndex(
                name: "IX_Menegar_IdSchool",
                table: "Menegar");

            migrationBuilder.DropIndex(
                name: "IX_Grades_IdClass",
                table: "Grades");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Attendan__3213E83FAD8350D2",
                table: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_IdClass",
                table: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_IdLectuer",
                table: "Attendance");

            migrationBuilder.DropIndex(
                name: "IX_Attendance_IdTeacher",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "IdSchool",
                table: "Teacher");

            migrationBuilder.DropColumn(
                name: "IdSchool",
                table: "Student");

            migrationBuilder.DropColumn(
                name: "IdSchool",
                table: "Menegar");

            migrationBuilder.DropColumn(
                name: "IdClass",
                table: "Grades");

            migrationBuilder.DropColumn(
                name: "AttendanceStatus",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "DateAndTime",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "Excuse",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "IdClass",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "IdLectuer",
                table: "Attendance");

            migrationBuilder.DropColumn(
                name: "IdTeacher",
                table: "Attendance");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Attendance",
                newName: "AttendanceID");

            migrationBuilder.AlterColumn<int>(
                name: "IdTeacher",
                table: "TeacherLectuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdLectuer",
                table: "TeacherLectuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdTeacher",
                table: "TeacherClass",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdClass",
                table: "TeacherClass",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdTeacher",
                table: "StudentTeacher",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdStudent",
                table: "StudentTeacher",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdStudent",
                table: "StudentLectuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdLectuer",
                table: "StudentLectuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdStudent",
                table: "StudentClass",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdClass",
                table: "StudentClass",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SecondMonth",
                table: "Grades",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Mid",
                table: "Grades",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "FirstMonth",
                table: "Grades",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Final",
                table: "Grades",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Activity",
                table: "Grades",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldDefaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "IdLectuer",
                table: "ClassLectuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdClass",
                table: "ClassLectuer",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "IdStudent",
                table: "Attendance",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AbsentDays",
                table: "Attendance",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "PresentDays",
                table: "Attendance",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "Total",
                table: "Grades",
                type: "int",
                nullable: true,
                computedColumnSql: "(((([FirstMonth]+[Mid])+[SecondMonth])+[Activity])+[Final])",
                stored: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true,
                oldComputedColumnSql: "(((([FirstMonth]+[Mid])+[SecondMonth])+[Activity])+[Final])",
                oldStored: false);

            migrationBuilder.AddColumn<int>(
                name: "TotalDays",
                table: "Attendance",
                type: "int",
                nullable: true,
                computedColumnSql: "([PresentDays]+[AbsentDays])",
                stored: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK__Attendan__8B69263C0DDD08A8",
                table: "Attendance",
                column: "AttendanceID");

            migrationBuilder.AddForeignKey(
                name: "FK__Attendanc__IdStu__09746778",
                table: "Attendance",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__ClassLect__IdCla__66603565",
                table: "ClassLectuer",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__ClassLect__IdLec__6754599E",
                table: "ClassLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__Grades__IdLectue__7EF6D905",
                table: "Grades",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__Grades__IdStuden__7D0E9093",
                table: "Grades",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__Grades__IdTeache__7E02B4CC",
                table: "Grades",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__StudentCl__IdCla__5812160E",
                table: "StudentClass",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__StudentCl__IdStu__571DF1D5",
                table: "StudentClass",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__StudentLe__IdLec__5BE2A6F2",
                table: "StudentLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__StudentLe__IdStu__5AEE82B9",
                table: "StudentLectuer",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__StudentTe__IdStu__534D60F1",
                table: "StudentTeacher",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__StudentTe__IdTea__5441852A",
                table: "StudentTeacher",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherCl__IdCla__5FB337D6",
                table: "TeacherClass",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherCl__IdTea__5EBF139D",
                table: "TeacherClass",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherLe__IdLec__6383C8BA",
                table: "TeacherLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherLe__IdTea__628FA481",
                table: "TeacherLectuer",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id");
        }
    }
}
