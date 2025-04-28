using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class FixGradeNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Lectuer_LectuerId",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Student_StudentId",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Teacher_TeacherId",
                table: "Grades");

            migrationBuilder.RenameColumn(
                name: "TeacherId",
                table: "Grades",
                newName: "TeacherId1");

            migrationBuilder.RenameColumn(
                name: "StudentId",
                table: "Grades",
                newName: "StudentId1");

            migrationBuilder.RenameColumn(
                name: "LectuerId",
                table: "Grades",
                newName: "LectuerId1");

            migrationBuilder.RenameIndex(
                name: "IX_Grades_TeacherId",
                table: "Grades",
                newName: "IX_Grades_TeacherId1");

            migrationBuilder.RenameIndex(
                name: "IX_Grades_StudentId",
                table: "Grades",
                newName: "IX_Grades_StudentId1");

            migrationBuilder.RenameIndex(
                name: "IX_Grades_LectuerId",
                table: "Grades",
                newName: "IX_Grades_LectuerId1");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Lectuer_LectuerId1",
                table: "Grades",
                column: "LectuerId1",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Student_StudentId1",
                table: "Grades",
                column: "StudentId1",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Teacher_TeacherId1",
                table: "Grades",
                column: "TeacherId1",
                principalTable: "Teacher",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Lectuer_LectuerId1",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Student_StudentId1",
                table: "Grades");

            migrationBuilder.DropForeignKey(
                name: "FK_Grades_Teacher_TeacherId1",
                table: "Grades");

            migrationBuilder.RenameColumn(
                name: "TeacherId1",
                table: "Grades",
                newName: "TeacherId");

            migrationBuilder.RenameColumn(
                name: "StudentId1",
                table: "Grades",
                newName: "StudentId");

            migrationBuilder.RenameColumn(
                name: "LectuerId1",
                table: "Grades",
                newName: "LectuerId");

            migrationBuilder.RenameIndex(
                name: "IX_Grades_TeacherId1",
                table: "Grades",
                newName: "IX_Grades_TeacherId");

            migrationBuilder.RenameIndex(
                name: "IX_Grades_StudentId1",
                table: "Grades",
                newName: "IX_Grades_StudentId");

            migrationBuilder.RenameIndex(
                name: "IX_Grades_LectuerId1",
                table: "Grades",
                newName: "IX_Grades_LectuerId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Lectuer_LectuerId",
                table: "Grades",
                column: "LectuerId",
                principalTable: "Lectuer",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Student_StudentId",
                table: "Grades",
                column: "StudentId",
                principalTable: "Student",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_Grades_Teacher_TeacherId",
                table: "Grades",
                column: "TeacherId",
                principalTable: "Teacher",
                principalColumn: "id");
        }
    }
}
