using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Gateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class OwnerDraftAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "DocumentId",
                table: "AuditEvents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "DocumentRevision",
                table: "AuditEvents",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_DocumentId_TenantId",
                table: "AuditEvents",
                columns: new[] { "DocumentId", "TenantId" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditEvents_TenantId_DocumentId_At",
                table: "AuditEvents",
                columns: new[] { "TenantId", "DocumentId", "At" });

            migrationBuilder.AddForeignKey(
                name: "FK_AuditEvents_Documents_DocumentId_TenantId",
                table: "AuditEvents",
                columns: new[] { "DocumentId", "TenantId" },
                principalTable: "Documents",
                principalColumns: new[] { "Id", "TenantId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AuditEvents_Documents_DocumentId_TenantId",
                table: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_DocumentId_TenantId",
                table: "AuditEvents");

            migrationBuilder.DropIndex(
                name: "IX_AuditEvents_TenantId_DocumentId_At",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "DocumentId",
                table: "AuditEvents");

            migrationBuilder.DropColumn(
                name: "DocumentRevision",
                table: "AuditEvents");
        }
    }
}
