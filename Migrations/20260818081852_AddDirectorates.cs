using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDirectorates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DirectorateId",
                table: "School",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "School",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "Directorate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Directorate", x => x.Id);
                });

            // Preserve every pre-existing school without guessing a real administrative owner.
            // Load-test schools are reassigned across seeded directorates by LoadTestDataSeeder.
            migrationBuilder.InsertData(
                table: "Directorate",
                columns: new[] { "Id", "Code", "Name", "IsActive", "CreatedAtUtc" },
                values: new object[]
                {
                    new Guid("11111111-1111-1111-1111-111111111111"), "LEGACY",
                    "مديرية انتقالية للمدارس السابقة", true, new DateTime(2026, 8, 18, 0, 0, 0, DateTimeKind.Utc)
                });

            migrationBuilder.Sql("UPDATE [School] SET [DirectorateId] = '11111111-1111-1111-1111-111111111111', [IsActive] = 1");

            migrationBuilder.AlterColumn<Guid>(
                name: "DirectorateId",
                table: "School",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "DirectorateManager",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DirectorateId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ApplicationUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IdNumber = table.Column<int>(type: "int", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Area = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DirectorateManager", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DirectorateManager_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_DirectorateManager_Directorate_DirectorateId",
                        column: x => x.DirectorateId,
                        principalTable: "Directorate",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_School_DirectorateId",
                table: "School",
                column: "DirectorateId");

            migrationBuilder.CreateIndex(
                name: "IX_Directorate_Code",
                table: "Directorate",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DirectorateManager_ApplicationUserId",
                table: "DirectorateManager",
                column: "ApplicationUserId",
                unique: true,
                filter: "[ApplicationUserId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_DirectorateManager_DirectorateId",
                table: "DirectorateManager",
                column: "DirectorateId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_School_Directorate_DirectorateId",
                table: "School",
                column: "DirectorateId",
                principalTable: "Directorate",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_School_Directorate_DirectorateId",
                table: "School");

            migrationBuilder.DropTable(
                name: "DirectorateManager");

            migrationBuilder.DropTable(
                name: "Directorate");

            migrationBuilder.DropIndex(
                name: "IX_School_DirectorateId",
                table: "School");

            migrationBuilder.DropColumn(
                name: "DirectorateId",
                table: "School");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "School");
        }
    }
}
