using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGradeOnly : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__ClassLect__IdCla__66603565",
                table: "ClassLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK__ClassLect__IdLec__6754599E",
                table: "ClassLectuer");

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
                name: "PK_Acounts",
                table: "Acounts");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Acounts",
                newName: "id");

            migrationBuilder.AlterColumn<string>(
                name: "UsersName",
                table: "Acounts",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Acounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Acounts",
                type: "datetime",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ResetToken",
                table: "Acounts",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Passwords",
                table: "Acounts",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Acounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddPrimaryKey(
                name: "PK__Acounts__3213E83F1B5AE3B5",
                table: "Acounts",
                column: "id");

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    AttendanceID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PresentDays = table.Column<int>(type: "int", nullable: false),
                    AbsentDays = table.Column<int>(type: "int", nullable: false),
                    TotalDays = table.Column<int>(type: "int", nullable: true, computedColumnSql: "([PresentDays]+[AbsentDays])", stored: true),
                    IdStudent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__8B69263C0DDD08A8", x => x.AttendanceID);
                    table.ForeignKey(
                        name: "FK__Attendanc__IdStu__09746778",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    GradesID = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstMonth = table.Column<int>(type: "int", nullable: true),
                    Mid = table.Column<int>(type: "int", nullable: true),
                    SecondMonth = table.Column<int>(type: "int", nullable: true),
                    Activity = table.Column<int>(type: "int", nullable: true),
                    Final = table.Column<int>(type: "int", nullable: true),
                    Total = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(((([FirstMonth]+[Mid])+[SecondMonth])+[Activity])+[Final])", stored: true),
                    IdStudent = table.Column<int>(type: "int", nullable: true),
                    IdTeacher = table.Column<int>(type: "int", nullable: true),
                    IdLectuer = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Grades__931A40BF88D8CDCA", x => x.GradesID);
                    table.ForeignKey(
                        name: "FK__Grades__IdLectue__7EF6D905",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Grades__IdStuden__7D0E9093",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Grades__IdTeache__7E02B4CC",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "StudentAverage",
                columns: table => new
                {
                    IdStudentAvg = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AverageGrade = table.Column<double>(type: "float", nullable: false),
                    IdStudent = table.Column<int>(type: "int", nullable: true),
                    IdClass = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentA__9A002AFC4CA4B91A", x => x.IdStudentAvg);
                    table.ForeignKey(
                        name: "FK__StudentAv__IdCla__03BB8E22",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__StudentAv__IdStu__02C769E9",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdStudent",
                table: "Attendance",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdLectuer",
                table: "Grades",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdStudent",
                table: "Grades",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdTeacher",
                table: "Grades",
                column: "IdTeacher");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAverage_IdClass",
                table: "StudentAverage",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAverage_IdStudent",
                table: "StudentAverage",
                column: "IdStudent");

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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK__ClassLect__IdCla__66603565",
                table: "ClassLectuer");

            migrationBuilder.DropForeignKey(
                name: "FK__ClassLect__IdLec__6754599E",
                table: "ClassLectuer");

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

            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "StudentAverage");

            migrationBuilder.DropPrimaryKey(
                name: "PK__Acounts__3213E83F1B5AE3B5",
                table: "Acounts");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "Acounts",
                newName: "Id");

            migrationBuilder.AlterColumn<string>(
                name: "UsersName",
                table: "Acounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Acounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Acounts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime");

            migrationBuilder.AlterColumn<string>(
                name: "ResetToken",
                table: "Acounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200);

            migrationBuilder.AlterColumn<string>(
                name: "Passwords",
                table: "Acounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Acounts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_Acounts",
                table: "Acounts",
                column: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK__ClassLect__IdCla__66603565",
                table: "ClassLectuer",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__ClassLect__IdLec__6754599E",
                table: "ClassLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__StudentCl__IdCla__5812160E",
                table: "StudentClass",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__StudentCl__IdStu__571DF1D5",
                table: "StudentClass",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__StudentLe__IdLec__5BE2A6F2",
                table: "StudentLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__StudentLe__IdStu__5AEE82B9",
                table: "StudentLectuer",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__StudentTe__IdStu__534D60F1",
                table: "StudentTeacher",
                column: "IdStudent",
                principalTable: "Student",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__StudentTe__IdTea__5441852A",
                table: "StudentTeacher",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherCl__IdCla__5FB337D6",
                table: "TeacherClass",
                column: "IdClass",
                principalTable: "TheClass",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherCl__IdTea__5EBF139D",
                table: "TeacherClass",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherLe__IdLec__6383C8BA",
                table: "TeacherLectuer",
                column: "IdLectuer",
                principalTable: "Lectuer",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK__TeacherLe__IdTea__628FA481",
                table: "TeacherLectuer",
                column: "IdTeacher",
                principalTable: "Teacher",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
