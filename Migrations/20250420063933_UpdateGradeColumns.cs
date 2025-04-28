using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGradeColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "UsersName",
                table: "Acounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Acounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Acounts",
                type: "datetime2",
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
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Acounts",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    AttendanceId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PresentDays = table.Column<int>(type: "int", nullable: false),
                    AbsentDays = table.Column<int>(type: "int", nullable: false),
                    TotalDays = table.Column<int>(type: "int", nullable: true, computedColumnSql: "[PresentDays] + [AbsentDays]", stored: true),
                    IdStudent = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Attendance", x => x.AttendanceId);
                    table.ForeignKey(
                        name: "FK_Attendance_Student_IdStudent",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    GradesId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstMonth = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Mid = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    SecondMonth = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Activity = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Final = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    Total = table.Column<int>(type: "int", nullable: true, computedColumnSql: "[FirstMonth] + [Mid] + [SecondMonth] + [Activity] + [Final]", stored: true),
                    IdStudent = table.Column<int>(type: "int", nullable: true),
                    IdTeacher = table.Column<int>(type: "int", nullable: true),
                    IdLectuer = table.Column<int>(type: "int", nullable: true),
                    LectuerId = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    TeacherId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grades", x => x.GradesId);
                    table.ForeignKey(
                        name: "FK_Grades_Lectuer_IdLectuer",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Grades_Lectuer_LectuerId",
                        column: x => x.LectuerId,
                        principalTable: "Lectuer",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Grades_Student_IdStudent",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Grades_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Grades_Teacher_IdTeacher",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Grades_Teacher_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teacher",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "StudentAverages",
                columns: table => new
                {
                    IdStudentAvg = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AverageGrade = table.Column<double>(type: "float", nullable: false, defaultValue: 0.0),
                    IdStudent = table.Column<int>(type: "int", nullable: true),
                    IdClass = table.Column<int>(type: "int", nullable: true),
                    StudentId = table.Column<int>(type: "int", nullable: true),
                    TheClassId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAverages", x => x.IdStudentAvg);
                    table.ForeignKey(
                        name: "FK_StudentAverages_Student_IdStudent",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_StudentAverages_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_StudentAverages_TheClass_IdClass",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_StudentAverages_TheClass_TheClassId",
                        column: x => x.TheClassId,
                        principalTable: "TheClass",
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
                name: "IX_Grades_LectuerId",
                table: "Grades",
                column: "LectuerId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_StudentId",
                table: "Grades",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_TeacherId",
                table: "Grades",
                column: "TeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAverages_IdClass",
                table: "StudentAverages",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAverages_IdStudent",
                table: "StudentAverages",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAverages_StudentId",
                table: "StudentAverages",
                column: "StudentId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAverages_TheClassId",
                table: "StudentAverages",
                column: "TheClassId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "StudentAverages");

            migrationBuilder.AlterColumn<string>(
                name: "UsersName",
                table: "Acounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<string>(
                name: "Role",
                table: "Acounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);

            migrationBuilder.AlterColumn<DateTime>(
                name: "ResetTokenExpiry",
                table: "Acounts",
                type: "datetime2",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

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
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "Acounts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50);
        }
    }
}
