using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SchoolSystem.Migrations
{
    /// <inheritdoc />
    public partial class BackfillOrganizationAssignments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SubjectIdentityNumber",
                table: "TransferRequest",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(@"
                DELETE d FROM [Directorate] d
                WHERE d.[Code] = 'LEGACY'
                  AND NOT EXISTS (SELECT 1 FROM [School] s WHERE s.[DirectorateId] = d.[Id])
                  AND NOT EXISTS (SELECT 1 FROM [DirectorateManager] dm WHERE dm.[DirectorateId] = d.[Id]);

                INSERT INTO [TeacherPlacement] ([Id], [TeacherId], [SchoolId], [IsPrimary], [IsActive], [StartedAtUtc], [EndedAtUtc])
                SELECT NEWID(), t.[id], t.[IdSchool], 1,
                       CASE WHEN t.[IsDeleted] = 0 AND t.[IsDeletedSchool] = 0 THEN 1 ELSE 0 END,
                       SYSUTCDATETIME(), CASE WHEN t.[IsDeleted] = 0 AND t.[IsDeletedSchool] = 0 THEN NULL ELSE SYSUTCDATETIME() END
                FROM [Teacher] t
                INNER JOIN [School] s ON s.[Id] = t.[IdSchool]
                WHERE t.[IdSchool] IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [TeacherPlacement] p WHERE p.[TeacherId] = t.[id] AND p.[SchoolId] = t.[IdSchool]);

                INSERT INTO [SchoolManagerAssignment] ([Id], [ManagerId], [SchoolId], [IsPrimary], [IsActive], [StartedAtUtc], [EndedAtUtc])
                SELECT NEWID(), m.[id], m.[IdSchool], 1,
                       CASE WHEN m.[IsDeleted] = 0 AND m.[IsDeletedSchool] = 0 THEN 1 ELSE 0 END,
                       SYSUTCDATETIME(), CASE WHEN m.[IsDeleted] = 0 AND m.[IsDeletedSchool] = 0 THEN NULL ELSE SYSUTCDATETIME() END
                FROM [Menegar] m
                INNER JOIN [School] s ON s.[Id] = m.[IdSchool]
                WHERE m.[IdSchool] IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [SchoolManagerAssignment] a WHERE a.[ManagerId] = m.[id] AND a.[SchoolId] = m.[IdSchool]);

                INSERT INTO [StudentEnrollment] ([Id], [StudentId], [SchoolId], [ClassId], [IsActive], [StartedAtUtc], [EndedAtUtc])
                SELECT NEWID(), st.[id], st.[IdSchool], st.[IdClass],
                       CASE WHEN st.[IsDeletedStudent] = 0 AND st.[IsDeletedSchool] = 0 AND st.[IsDeletedClass] = 0 THEN 1 ELSE 0 END,
                       SYSUTCDATETIME(), CASE WHEN st.[IsDeletedStudent] = 0 AND st.[IsDeletedSchool] = 0 AND st.[IsDeletedClass] = 0 THEN NULL ELSE SYSUTCDATETIME() END
                FROM [Student] st
                INNER JOIN [School] s ON s.[Id] = st.[IdSchool]
                INNER JOIN [TheClass] c ON c.[id] = st.[IdClass] AND c.[IdSchool] = st.[IdSchool]
                WHERE st.[IdSchool] IS NOT NULL AND st.[IdClass] IS NOT NULL
                  AND NOT EXISTS (SELECT 1 FROM [StudentEnrollment] e WHERE e.[StudentId] = st.[id]);");
            migrationBuilder.CreateIndex(
                name: "IX_TransferRequest_SubjectType_SubjectIdentityNumber_Status",
                table: "TransferRequest",
                columns: new[] { "SubjectType", "SubjectIdentityNumber", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TransferRequest_SubjectType_SubjectIdentityNumber_Status",
                table: "TransferRequest");

            migrationBuilder.DropColumn(
                name: "SubjectIdentityNumber",
                table: "TransferRequest");
        }
    }
}
