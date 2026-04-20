using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shsmg.Pharma.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceAuditLogging : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InvoiceAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    InvoiceNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Action = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SnapshotJson = table.Column<string>(type: "text", nullable: false),
                    PerformedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    PerformedBy = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InvoiceAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAuditLogs_InvoiceId",
                table: "InvoiceAuditLogs",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceAuditLogs_PerformedAt",
                table: "InvoiceAuditLogs",
                column: "PerformedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InvoiceAuditLogs");
        }
    }
}
