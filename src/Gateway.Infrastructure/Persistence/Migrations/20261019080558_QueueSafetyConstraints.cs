using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QueueSafetyConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddCheckConstraint(
                name: "CK_ExportJobs_Attempts",
                table: "ExportJobs",
                sql: "[AttemptCount] BETWEEN 0 AND 3 AND [Fence] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExportJobs_Mode",
                table: "ExportJobs",
                sql: "[Mode] IN (0,1) AND ([State] <> 9 OR [Mode] = 0)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExportJobs_Snapshot",
                table: "ExportJobs",
                sql: "ISJSON([SnapshotJson]) = 1");

            migrationBuilder.AddCheckConstraint(
                name: "CK_ExportJobs_SourceKind",
                table: "ExportJobs",
                sql: "[SourceKind] IN (0,1)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_ExportJobs_Attempts",
                table: "ExportJobs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExportJobs_Mode",
                table: "ExportJobs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExportJobs_Snapshot",
                table: "ExportJobs");

            migrationBuilder.DropCheckConstraint(
                name: "CK_ExportJobs_SourceKind",
                table: "ExportJobs");
        }
    }
}
