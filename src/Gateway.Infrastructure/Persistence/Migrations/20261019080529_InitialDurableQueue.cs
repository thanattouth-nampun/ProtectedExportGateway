using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialDurableQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExportJobs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterOid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IdempotencyKey = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequestDigest = table.Column<string>(type: "varchar(64)", unicode: false, maxLength: 64, nullable: false, collation: "Latin1_General_100_BIN2"),
                    SourceKind = table.Column<int>(type: "int", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false),
                    SnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    AvailableAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Fence = table.Column<long>(type: "bigint", nullable: false),
                    AttemptCount = table.Column<int>(type: "int", nullable: false),
                    LeaseOwner = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    LeaseToken = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    LeaseExpiresAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    SafeErrorCode = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportJobs", x => x.Id);
                    table.CheckConstraint("CK_ExportJobs_Digest", "LEN([RequestDigest]) = 64");
                    table.CheckConstraint("CK_ExportJobs_Lease", "([LeaseToken] IS NULL AND [LeaseOwner] IS NULL AND [LeaseExpiresAt] IS NULL) OR ([LeaseToken] IS NOT NULL AND [LeaseOwner] IS NOT NULL AND [LeaseExpiresAt] IS NOT NULL)");
                    table.CheckConstraint("CK_ExportJobs_SafeStates", "[State] IN (0,1,2,3,4,6,7,9)");
                });

            migrationBuilder.CreateTable(
                name: "AuditEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ActorOid = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EventType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    At = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    CorrelationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Mode = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuditEvents_ExportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "ExportJobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "ExportAttempts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Number = table.Column<int>(type: "int", nullable: false),
                    Fence = table.Column<long>(type: "bigint", nullable: false),
                    Worker = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    EndedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExportAttempts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExportAttempts_ExportJobs_JobId",
                        column: x => x.JobId,
                        principalTable: "ExportJobs",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "DocumentAcls",
                columns: table => new
                {
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RequesterOid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CanRequestProtectedCopy = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentAcls", x => new { x.DocumentId, x.TenantId, x.RequesterOid });
                });

            migrationBuilder.CreateTable(
                name: "Documents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OwnerOid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Purpose = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Enabled = table.Column<bool>(type: "bit", nullable: false),
                    CurrentPublishedVersionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Revision = table.Column<long>(type: "bigint", nullable: false),
                    AclRevision = table.Column<long>(type: "bigint", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Documents", x => x.Id);
                    table.UniqueConstraint("AK_Documents_Id_TenantId", x => new { x.Id, x.TenantId });
                });

            migrationBuilder.CreateTable(
                name: "DocumentVersions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UploaderOid = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DisplayFilename = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    State = table.Column<int>(type: "int", nullable: false),
                    SourceBindingJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ScanEvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValidationEvidenceJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DocumentVersions", x => x.Id);
                    table.UniqueConstraint("AK_DocumentVersions_Id_DocumentId_TenantId", x => new { x.Id, x.DocumentId, x.TenantId });
                    table.ForeignKey(
                        name: "FK_DocumentVersions_Documents_DocumentId_TenantId",
                        columns: x => new { x.DocumentId, x.TenantId },
                        principalTable: "Documents",
                        principalColumns: new[] { "Id", "TenantId" });
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_JobId",
                table: "AuditEvents",
                column: "JobId");

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TenantId_At",
                table: "AuditEvents",
                columns: new[] { "TenantId", "At" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_CurrentPublishedVersionId_Id_TenantId",
                table: "Documents",
                columns: new[] { "CurrentPublishedVersionId", "Id", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TenantId_OwnerOid",
                table: "Documents",
                columns: new[] { "TenantId", "OwnerOid" });

            migrationBuilder.CreateIndex(
                name: "IX_DocumentVersions_DocumentId_TenantId",
                table: "DocumentVersions",
                columns: new[] { "DocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportAttempts_JobId_Fence",
                table: "ExportAttempts",
                columns: new[] { "JobId", "Fence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportAttempts_JobId_Number",
                table: "ExportAttempts",
                columns: new[] { "JobId", "Number" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_State_AvailableAt_LeaseExpiresAt",
                table: "ExportJobs",
                columns: new[] { "State", "AvailableAt", "LeaseExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_TenantId_RequesterOid_CreatedAt",
                table: "ExportJobs",
                columns: new[] { "TenantId", "RequesterOid", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ExportJobs_TenantId_RequesterOid_IdempotencyKey",
                table: "ExportJobs",
                columns: new[] { "TenantId", "RequesterOid", "IdempotencyKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_DocumentAcls_Documents_DocumentId_TenantId",
                table: "DocumentAcls",
                columns: new[] { "DocumentId", "TenantId" },
                principalTable: "Documents",
                principalColumns: new[] { "Id", "TenantId" });

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_DocumentVersions_CurrentPublishedVersionId_Id_TenantId",
                table: "Documents",
                columns: new[] { "CurrentPublishedVersionId", "Id", "TenantId" },
                principalTable: "DocumentVersions",
                principalColumns: new[] { "Id", "DocumentId", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_DocumentVersions_Documents_DocumentId_TenantId",
                table: "DocumentVersions");

            migrationBuilder.DropTable(
                name: "AuditEvents");

            migrationBuilder.DropTable(
                name: "DocumentAcls");

            migrationBuilder.DropTable(
                name: "ExportAttempts");

            migrationBuilder.DropTable(
                name: "ExportJobs");

            migrationBuilder.DropTable(
                name: "Documents");

            migrationBuilder.DropTable(
                name: "DocumentVersions");
        }
    }
}
