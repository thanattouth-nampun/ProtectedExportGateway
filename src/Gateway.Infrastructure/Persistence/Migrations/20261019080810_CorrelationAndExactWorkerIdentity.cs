using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CorrelationAndExactWorkerIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // SQL Server will not alter a column while a check constraint references it.
            migrationBuilder.DropCheckConstraint("CK_ExportJobs_Lease", "ExportJobs");
            migrationBuilder.AlterColumn<string>(
                name: "LeaseOwner",
                table: "ExportJobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                collation: "Latin1_General_100_BIN2",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint("CK_ExportJobs_Lease", "ExportJobs",
                "([LeaseToken] IS NULL AND [LeaseOwner] IS NULL AND [LeaseExpiresAt] IS NULL) OR ([LeaseToken] IS NOT NULL AND [LeaseOwner] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL)");

            migrationBuilder.AddColumn<Guid>(
                name: "CorrelationId",
                table: "ExportJobs",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint("CK_ExportJobs_Lease", "ExportJobs");
            migrationBuilder.DropColumn(
                name: "CorrelationId",
                table: "ExportJobs");

            migrationBuilder.AlterColumn<string>(
                name: "LeaseOwner",
                table: "ExportJobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true,
                oldCollation: "Latin1_General_100_BIN2");
            migrationBuilder.AddCheckConstraint("CK_ExportJobs_Lease", "ExportJobs",
                "([LeaseToken] IS NULL AND [LeaseOwner] IS NULL AND [LeaseExpiresAt] IS NULL) OR ([LeaseToken] IS NOT NULL AND [LeaseOwner] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL)");
        }
    }
}
