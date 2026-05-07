using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shsmg.Pharma.Infra.Migrations
{
    /// <inheritdoc />
    public partial class TrackInvoiceInventoryMovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "InventoryMovementApplied",
                table: "InvoiceItems",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InventoryMovementApplied",
                table: "InvoiceItems");
        }
    }
}
