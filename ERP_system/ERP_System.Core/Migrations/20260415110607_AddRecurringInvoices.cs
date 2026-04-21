using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_System.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddRecurringInvoices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "invoice_id",
                table: "repetable_transactions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_repetable_transactions_invoice_id",
                table: "repetable_transactions",
                column: "invoice_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_repetable_transactions_invoices_invoice_id",
                table: "repetable_transactions",
                column: "invoice_id",
                principalTable: "invoices",
                principalColumn: "invoice_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_repetable_transactions_invoices_invoice_id",
                table: "repetable_transactions");

            migrationBuilder.DropIndex(
                name: "IX_repetable_transactions_invoice_id",
                table: "repetable_transactions");

            migrationBuilder.DropColumn(
                name: "invoice_id",
                table: "repetable_transactions");
        }
    }
}
