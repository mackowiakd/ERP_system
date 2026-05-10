using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_System.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoryToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "invoices",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_invoices_category_id",
                table: "invoices",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_invoices_transaction_categories_category_id",
                table: "invoices",
                column: "category_id",
                principalTable: "transaction_categories",
                principalColumn: "category_id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_invoices_transaction_categories_category_id",
                table: "invoices");

            migrationBuilder.DropIndex(
                name: "IX_invoices_category_id",
                table: "invoices");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "invoices");
        }
    }
}
