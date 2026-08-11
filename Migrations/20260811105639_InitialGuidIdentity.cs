using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class InitialGuidIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Branch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BranchName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    BranchCode = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Branch__54205B04058BBB4F", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    LoggedAt = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "(getutcdate())")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ErrorLog__3214EC07A2529995", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Gender",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TheType = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Gender__3214EC070A22819D", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfileImage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ProfileImagePath = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__ProfileI__3214EC07AD185F4E", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StageClass",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false),
                    MinClass = table.Column<int>(type: "int", nullable: false),
                    MaxClass = table.Column<int>(type: "int", nullable: false),
                    NameStage = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StageCla__3214EC07B7F14D02", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StatusSchool",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    condition = table.Column<bool>(type: "bit", nullable: true),
                    TheType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StatusSc__3214EC073DB2CBA3", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RoleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "School",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IdStatusSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdGender = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    MinClass = table.Column<int>(type: "int", nullable: true),
                    MaxClass = table.Column<int>(type: "int", nullable: true),
                    IdStage = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
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
                        name: "FK__School__IdStage__377B294A",
                        column: x => x.IdStage,
                        principalTable: "StageClass",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__School__IdStatus__22FF2F51",
                        column: x => x.IdStatusSchool,
                        principalTable: "StatusSchool",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Lectuer",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Lectuer__3213E83FBA6843F5", x => x.id);
                    table.ForeignKey(
                        name: "FK__Lectuer__IdSchoo__6C6E1476",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Menegar",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TheDate = table.Column<DateOnly>(type: "date", nullable: true),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdNumber = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Menegar__3213E83FE96BFA1F", x => x.id);
                    table.ForeignKey(
                        name: "FK_Menegar_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__Menegar__IdSchoo__23F3538A",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Teacher",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TheDate = table.Column<DateOnly>(type: "date", nullable: true),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdNumber = table.Column<int>(type: "int", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Teacher__3213E83F92AB32EF", x => x.id);
                    table.ForeignKey(
                        name: "FK_Teacher_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__Teacher__IdSchoo__25DB9BFC",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TheClass",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false),
                    IdStage = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    NumberClass = table.Column<int>(type: "int", nullable: true),
                    Section = table.Column<int>(type: "int", nullable: true),
                    IdBranch = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TheClass__3213E83FD60CD186", x => x.id);
                    table.ForeignKey(
                        name: "FK_TheClass_School",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__TheClass__IdBran__3592E0D8",
                        column: x => x.IdBranch,
                        principalTable: "Branch",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK__TheClass__IdStag__32B6742D",
                        column: x => x.IdStage,
                        principalTable: "StageClass",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Student",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    Email = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TheDate = table.Column<DateOnly>(type: "date", nullable: true),
                    IdClass = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    City = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    IdNumber = table.Column<int>(type: "int", nullable: true),
                    IsDeletedStudent = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedClass = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Student__3213E83F6F20DDDC", x => x.id);
                    table.ForeignKey(
                        name: "FK_Student_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK__Student__IdClass__1A1FD08D",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Student__IdSchoo__24E777C3",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "TeacherLectuerClass",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdTeacher = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdLectuer = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdClass = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeletedTeacherLectuerClass = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedClass = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedLectuer = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedTeacher = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false),
                    IsTeacherRemovedFromClass = table.Column<bool>(type: "bit", nullable: false),
                    IsTeacherRemovedFromLectuer = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__TeacherL__3213E83F5B2FD59A", x => x.id);
                    table.ForeignKey(
                        name: "FK_TeacherLectuer_Lectuer",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_TeacherLectuer_Teacher",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__TeacherLe__IdCla__74EE4BDE",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__TeacherLe__IdSch__7D2E8C24",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Attendance",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AttendanceStatus = table.Column<string>(type: "char(1)", unicode: false, fixedLength: true, maxLength: 1, nullable: false, defaultValue: "0"),
                    DateAndTime = table.Column<DateOnly>(type: "date", nullable: true),
                    Excuse = table.Column<string>(type: "text", nullable: true),
                    IdTeacher = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdLectuer = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdStudent = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdClass = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeletedAttendance = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedClass = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedTeacher = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedStudent = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedLectuer = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false),
                    IsTeacherRemovedFromClass = table.Column<bool>(type: "bit", nullable: false),
                    IsTeacherRemovedFromLectuer = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Attendan__3213E83FAD8350D2", x => x.id);
                    table.ForeignKey(
                        name: "FK_Attendance_Lectuer",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Attendance_School",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Attendance_Student",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Attendance_Teacher",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Attendance_TheClass",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "Grades",
                columns: table => new
                {
                    GradesID = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstMonth = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Mid = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    SecondMonth = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Activity = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    Final = table.Column<int>(type: "int", nullable: true, defaultValue: 0),
                    IdStudent = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdTeacher = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdLectuer = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdClass = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Total = table.Column<int>(type: "int", nullable: true, computedColumnSql: "(((([FirstMonth]+[Mid])+[SecondMonth])+[Activity])+[Final])", stored: false),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeletedGrades = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedClass = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedLectuer = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedStudent = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedTeacher = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false),
                    IsTeacherRemovedFromClass = table.Column<bool>(type: "bit", nullable: false),
                    IsTeacherRemovedFromLectuer = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__Grades__931A40BF88D8CDCA", x => x.GradesID);
                    table.ForeignKey(
                        name: "FK_Grades_Lectuer",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Grades_Student",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_Grades_Teacher",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Grades__IdClass__0D0FEE32",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__Grades__IdSchool__6D6238AF",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "StudentLectuerTeacher",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdStudent = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdLectuer = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdSchool = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdClass = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IdTeacher = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsDeletedStudentLectuerTeacher = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedClass = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedStudent = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedTeacher = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedSchool = table.Column<bool>(type: "bit", nullable: false),
                    IsDeletedLectuer = table.Column<bool>(type: "bit", nullable: false),
                    IsTeacherRemovedFromClass = table.Column<bool>(type: "bit", nullable: false),
                    IsTeacherRemovedFromLectuer = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK__StudentL__3213E83F373E3AFF", x => x.id);
                    table.ForeignKey(
                        name: "FK_StudentLectuer_Lectuer",
                        column: x => x.IdLectuer,
                        principalTable: "Lectuer",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_StudentLectuer_School",
                        column: x => x.IdSchool,
                        principalTable: "School",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_StudentLectuer_Student",
                        column: x => x.IdStudent,
                        principalTable: "Student",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__StudentLe__IdCla__08F5448B",
                        column: x => x.IdClass,
                        principalTable: "TheClass",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK__StudentLe__IdTea__09E968C4",
                        column: x => x.IdTeacher,
                        principalTable: "Teacher",
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdClass",
                table: "Attendance",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdLectuer",
                table: "Attendance",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdSchool",
                table: "Attendance",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdStudent",
                table: "Attendance",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_Attendance_IdTeacher",
                table: "Attendance",
                column: "IdTeacher");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_Id",
                table: "Grades",
                column: "GradesID");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdClass",
                table: "Grades",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdLectuer",
                table: "Grades",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdSchool",
                table: "Grades",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdStudent",
                table: "Grades",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_Grades_IdTeacher",
                table: "Grades",
                column: "IdTeacher");

            migrationBuilder.CreateIndex(
                name: "IX_Lectuer_IdSchool",
                table: "Lectuer",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Lectuer_Name",
                table: "Lectuer",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_Menegar_ApplicationUserId",
                table: "Menegar",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Menegar_IdSchool",
                table: "Menegar",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Menegar_Name",
                table: "Menegar",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "UQ_Menegar_IdNumber",
                table: "Menegar",
                column: "IdNumber",
                unique: true,
                filter: "[IdNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_School_IdGender",
                table: "School",
                column: "IdGender");

            migrationBuilder.CreateIndex(
                name: "IX_School_IdStage",
                table: "School",
                column: "IdStage");

            migrationBuilder.CreateIndex(
                name: "IX_School_IdStatusSchool",
                table: "School",
                column: "IdStatusSchool");

            migrationBuilder.CreateIndex(
                name: "UQ__StageCla__A25C5AA7AAECF2F1",
                table: "StageClass",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Student_ApplicationUserId",
                table: "Student",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Student_IdClass",
                table: "Student",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_Student_IdSchool",
                table: "Student",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Student_Name",
                table: "Student",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLectuer_IdSchool",
                table: "StudentLectuerTeacher",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLectuerTeacher_IdClass",
                table: "StudentLectuerTeacher",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLectuerTeacher_IdLectuer",
                table: "StudentLectuerTeacher",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLectuerTeacher_IdStudent",
                table: "StudentLectuerTeacher",
                column: "IdStudent");

            migrationBuilder.CreateIndex(
                name: "IX_StudentLectuerTeacher_IdTeacher",
                table: "StudentLectuerTeacher",
                column: "IdTeacher");

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_ApplicationUserId",
                table: "Teacher",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_IdSchool",
                table: "Teacher",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_Teacher_Name",
                table: "Teacher",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "UQ_Teacher_IdNumber",
                table: "Teacher",
                column: "IdNumber",
                unique: true,
                filter: "[IdNumber] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLectuer_IdSchool",
                table: "TeacherLectuerClass",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLectuerClass_IdClass",
                table: "TeacherLectuerClass",
                column: "IdClass");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLectuerClass_IdLectuer",
                table: "TeacherLectuerClass",
                column: "IdLectuer");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherLectuerClass_IdTeacher",
                table: "TeacherLectuerClass",
                column: "IdTeacher");

            migrationBuilder.CreateIndex(
                name: "IX_TheClass_IdBranch",
                table: "TheClass",
                column: "IdBranch");

            migrationBuilder.CreateIndex(
                name: "IX_TheClass_IdSchool",
                table: "TheClass",
                column: "IdSchool");

            migrationBuilder.CreateIndex(
                name: "IX_TheClass_IdStage",
                table: "TheClass",
                column: "IdStage");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "Attendance");

            migrationBuilder.DropTable(
                name: "ErrorLogs");

            migrationBuilder.DropTable(
                name: "Grades");

            migrationBuilder.DropTable(
                name: "Menegar");

            migrationBuilder.DropTable(
                name: "ProfileImage");

            migrationBuilder.DropTable(
                name: "StudentLectuerTeacher");

            migrationBuilder.DropTable(
                name: "TeacherLectuerClass");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Student");

            migrationBuilder.DropTable(
                name: "Lectuer");

            migrationBuilder.DropTable(
                name: "Teacher");

            migrationBuilder.DropTable(
                name: "TheClass");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "School");

            migrationBuilder.DropTable(
                name: "Branch");

            migrationBuilder.DropTable(
                name: "Gender");

            migrationBuilder.DropTable(
                name: "StageClass");

            migrationBuilder.DropTable(
                name: "StatusSchool");
        }
    }
}
