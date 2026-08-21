using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddMinistriesTransfersAndAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EducationLevel",
                table: "School",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Primary");

            migrationBuilder.AddColumn<string>(
                name: "OwnershipType",
                table: "School",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "Government");

            migrationBuilder.AddColumn<Guid>(
                name: "MinistryId",
                table: "Directorate",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "Ministry",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ministry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SchoolManagerAssignment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ManagerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SchoolManagerAssignment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SchoolManagerAssignment_Menegar_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Menegar",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SchoolManagerAssignment_School_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "School",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "StudentEnrollment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StudentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEnrollment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentEnrollment_School_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "School",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollment_Student_StudentId",
                        column: x => x.StudentId,
                        principalTable: "Student",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_StudentEnrollment_TheClass_ClassId",
                        column: x => x.ClassId,
                        principalTable: "TheClass",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TeacherPlacement",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeacherId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeacherPlacement", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeacherPlacement_School_SchoolId",
                        column: x => x.SchoolId,
                        principalTable: "School",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TeacherPlacement_Teacher_TeacherId",
                        column: x => x.TeacherId,
                        principalTable: "Teacher",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TransferRequest",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubjectType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    SourceMinistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationMinistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceDirectorateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationDirectorateId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceSchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationSchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SourceClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DestinationClassId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RequestedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DecidedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DecisionNote = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DecidedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferRequest", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MinistryManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinistryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IdNumber = table.Column<int>(type: "int", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MinistryManager", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MinistryManager_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MinistryManager_Ministry_MinistryId",
                        column: x => x.MinistryId,
                        principalTable: "Ministry",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "UX_TeacherLectuer_ActiveSchoolClassSubject",
                table: "TeacherLectuerClass",
                columns: new[] { "IdSchool", "IdClass", "IdLectuer" },
                unique: true,
                filter: "[IsDeletedTeacherLectuerClass] = 0 AND [IsDeletedClass] = 0 AND [IsDeletedLectuer] = 0 AND [IsDeletedTeacher] = 0 AND [IsDeletedSchool] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Directorate_MinistryId",
                table: "Directorate",
                column: "MinistryId");

            migrationBuilder.CreateIndex(
                name: "IX_Ministry_Code",
                table: "Ministry",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MinistryManager_ApplicationUserId",
                table: "MinistryManager",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MinistryManager_MinistryId",
                table: "MinistryManager",
                column: "MinistryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SchoolManagerAssignment_ManagerId",
                table: "SchoolManagerAssignment",
                column: "ManagerId",
                unique: true,
                filter: "[IsActive] = 1 AND [IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolManagerAssignment_ManagerId_SchoolId",
                table: "SchoolManagerAssignment",
                columns: new[] { "ManagerId", "SchoolId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_SchoolManagerAssignment_SchoolId",
                table: "SchoolManagerAssignment",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollment_ClassId",
                table: "StudentEnrollment",
                column: "ClassId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollment_SchoolId",
                table: "StudentEnrollment",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEnrollment_StudentId",
                table: "StudentEnrollment",
                column: "StudentId",
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherPlacement_SchoolId",
                table: "TeacherPlacement",
                column: "SchoolId");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherPlacement_TeacherId",
                table: "TeacherPlacement",
                column: "TeacherId",
                unique: true,
                filter: "[IsActive] = 1 AND [IsPrimary] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TeacherPlacement_TeacherId_SchoolId",
                table: "TeacherPlacement",
                columns: new[] { "TeacherId", "SchoolId" },
                unique: true,
                filter: "[IsActive] = 1");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequest_DestinationDirectorateId",
                table: "TransferRequest",
                column: "DestinationDirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequest_DestinationMinistryId",
                table: "TransferRequest",
                column: "DestinationMinistryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequest_SourceDirectorateId",
                table: "TransferRequest",
                column: "SourceDirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequest_SourceMinistryId",
                table: "TransferRequest",
                column: "SourceMinistryId");

            migrationBuilder.CreateIndex(
                name: "IX_TransferRequest_SubjectType_SubjectId_Status",
                table: "TransferRequest",
                columns: new[] { "SubjectType", "SubjectId", "Status" });

            migrationBuilder.AddForeignKey(
                name: "FK_Directorate_Ministry_MinistryId",
                table: "Directorate",
                column: "MinistryId",
                principalTable: "Ministry",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Directorate_Ministry_MinistryId",
                table: "Directorate");

            migrationBuilder.DropTable(
                name: "MinistryManager");

            migrationBuilder.DropTable(
                name: "SchoolManagerAssignment");

            migrationBuilder.DropTable(
                name: "StudentEnrollment");

            migrationBuilder.DropTable(
                name: "TeacherPlacement");

            migrationBuilder.DropTable(
                name: "TransferRequest");

            migrationBuilder.DropTable(
                name: "Ministry");

            migrationBuilder.DropIndex(
                name: "UX_TeacherLectuer_ActiveSchoolClassSubject",
                table: "TeacherLectuerClass");

            migrationBuilder.DropIndex(
                name: "IX_Directorate_MinistryId",
                table: "Directorate");

            migrationBuilder.DropColumn(
                name: "EducationLevel",
                table: "School");

            migrationBuilder.DropColumn(
                name: "OwnershipType",
                table: "School");

            migrationBuilder.DropColumn(
                name: "MinistryId",
                table: "Directorate");
        }
    }
}
