using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shsmg.Pharma.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddInventoryManagement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "InventoryItemId",
                table: "InvoiceItems",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "InventoryItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    HsnCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Package = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Mfg = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Batch = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ExpiryDate = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    QuantityInStock = table.Column<int>(type: "integer", nullable: false),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: false),
                    Rate = table.Column<decimal>(type: "numeric(12,2)", precision: 12, scale: 2, nullable: false),
                    GstPercentage = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<string>(type: "text", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastModifiedBy = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Batch",
                table: "InventoryItems",
                column: "Batch");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_Description",
                table: "InventoryItems",
                column: "Description");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryItems_HsnCode",
                table: "InventoryItems",
                column: "HsnCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryItems");

            migrationBuilder.DropColumn(
                name: "InventoryItemId",
                table: "InvoiceItems");
        }
    }
}
