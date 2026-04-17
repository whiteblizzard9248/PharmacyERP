using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Shsmg.Pharma.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddedNewHsn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HsnCode",
                table: "InvoiceItems",
                type: "character varying(10)",
                maxLength: 10,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_HsnCode",
                table: "InvoiceItems",
                column: "HsnCode");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_HsnCode",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "HsnCode",
                table: "InvoiceItems");
        }
    }
}
