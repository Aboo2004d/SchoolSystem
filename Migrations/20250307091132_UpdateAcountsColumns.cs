using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAcountsColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Acounts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsersName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Passwords = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Acounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Lectuer",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Lectuer__3213E83FBA6843F5", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Menegar",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Phone = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Menegar__3213E83FE96BFA1F", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Phone = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student__3213E83F6F20DDDC", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Teacher",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Phone = table.Column<int>(type: "int", nullable: false),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Teacher__3213E83F92AB32EF", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "TheClass",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TheClass__3213E83FD60CD186", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "StudentLectuer",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdStudent = table.Column<int>(type: "int", nullable: false),
                    IdLectuer = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentL__3213E83F373E3AFF", x => x.id);
                    table.ForeignKey(
                        name: "FK__StudentLe__IdLec__5BE2A6F2",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__StudentLe__IdStu__5AEE82B9",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentTeacher",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdStudent = table.Column<int>(type: "int", nullable: false),
                    IdTeacher = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentT__3213E83F7AD0C21E", x => x.id);
                    table.ForeignKey(
                        name: "FK__StudentTe__IdStu__534D60F1",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__StudentTe__IdTea__5441852A",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherLectuer",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTeacher = table.Column<int>(type: "int", nullable: false),
                    IdLectuer = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TeacherL__3213E83F5B2FD59A", x => x.id);
                    table.ForeignKey(
                        name: "FK__TeacherLe__IdLec__6383C8BA",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__TeacherLe__IdTea__628FA481",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClassLectuer",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdClass = table.Column<int>(type: "int", nullable: false),
                    IdLectuer = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ClassLec__3213E83F8327C66B", x => x.id);
                    table.ForeignKey(
                        name: "FK__ClassLect__IdCla__66603565",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__ClassLect__IdLec__6754599E",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentClass",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdStudent = table.Column<int>(type: "int", nullable: false),
                    IdClass = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentC__3213E83F95F82A92", x => x.id);
                    table.ForeignKey(
                        name: "FK__StudentCl__IdCla__5812160E",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__StudentCl__IdStu__571DF1D5",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeacherClass",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    IdTeacher = table.Column<int>(type: "int", nullable: false),
                    IdClass = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TeacherC__3213E83FDA6F83CA", x => x.id);
                    table.ForeignKey(
                        name: "FK__TeacherCl__IdCla__5FB337D6",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK__TeacherCl__IdTea__5EBF139D",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClassLectuer_IdClass",
                table: "ClassLectuer",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_ClassLectuer_IdLectuer",
                table: "ClassLectuer",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_StudentClass_IdClass",
                table: "StudentClass",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_StudentClass_IdStudent",
                table: "StudentClass",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLectuer_IdLectuer",
                table: "StudentLectuer",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLectuer_IdStudent",
                table: "StudentLectuer",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTeacher_IdStudent",
                table: "StudentTeacher",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_StudentTeacher_IdTeacher",
                table: "StudentTeacher",
                column: "IdTeacher");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClass_IdClass",
                table: "TeacherClass",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherClass_IdTeacher",
                table: "TeacherClass",
                column: "IdTeacher");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLectuer_IdLectuer",
                table: "TeacherLectuer",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLectuer_IdTeacher",
                table: "TeacherLectuer",
                column: "IdTeacher");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Acounts");

            migrationBuilder.DropTable(
                name: "ClassLectuer");

            migrationBuilder.DropTable(
                name: "Menegar");

            migrationBuilder.DropTable(
                name: "StudentClass");

            migrationBuilder.DropTable(
                name: "StudentLectuer");

            migrationBuilder.DropTable(
                name: "StudentTeacher");

            migrationBuilder.DropTable(
                name: "TeacherClass");

            migrationBuilder.DropTable(
                name: "TeacherLectuer");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "TheClass");

            migrationBuilder.DropTable(
                name: "Lectuer");

            migrationBuilder.DropTable(
                name: "Teacher");
        }
    }
}
