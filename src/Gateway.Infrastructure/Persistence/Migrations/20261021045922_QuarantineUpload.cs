using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class QuarantineUpload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "QuarantineBytes",
                table: "DocumentVersions",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "QuarantineSha256",
                table: "DocumentVersions",
                type: "varchar(64)",
                unicode: false,
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "QuarantineToken",
                table: "DocumentVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UploadAttempts",
                table: "DocumentVersions",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "UploadErrorCode",
                table: "DocumentVersions",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "UploadKey",
                table: "DocumentVersions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UploadLeaseUntil",
                table: "DocumentVersions",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_TenantId_UploaderOid_DocumentId_UploadKey",
                table: "DocumentVersions",
                columns: new[] { "TenantId", "UploaderOid", "DocumentId", "UploadKey" },
                unique: true,
                filter: "[UploadKey] IS NOT NULL");

            migrationBuilder.AddCheckConstraint(
                name: "CK_DocumentVersions_Quarantine",
                table: "DocumentVersions",
                sql: "[UploadKey] IS NULL OR ([UploadAttempts] BETWEEN 1 AND 3 AND [QuarantineToken] IS NOT NULL AND [State] IN (0,1,6) AND [SourceBindingJson] IS NULL AND [ScanEvidenceJson] IS NULL AND [ValidationEvidenceJson] IS NULL AND (([State]=0 AND [UploadLeaseUntil] IS NOT NULL) OR ([State]<>0 AND [UploadLeaseUntil] IS NULL)) AND ([State] <> 1 OR ([QuarantineBytes] IS NOT NULL AND [QuarantineBytes] BETWEEN 1 AND 10485760 AND [QuarantineSha256] IS NOT NULL AND LEN([QuarantineSha256])=64)))");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_DocumentVersions_TenantId_UploaderOid_DocumentId_UploadKey",
                table: "DocumentVersions");

            migrationBuilder.DropCheckConstraint(
                name: "CK_DocumentVersions_Quarantine",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "QuarantineBytes",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "QuarantineSha256",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "QuarantineToken",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "UploadAttempts",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "UploadErrorCode",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "UploadKey",
                table: "DocumentVersions");

            migrationBuilder.DropColumn(
                name: "UploadLeaseUntil",
                table: "DocumentVersions");
        }
    }
}
