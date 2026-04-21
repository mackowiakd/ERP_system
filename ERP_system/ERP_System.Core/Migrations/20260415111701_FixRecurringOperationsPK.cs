using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_System.Core.Migrations
{
    /// <inheritdoc />
    public partial class FixRecurringOperationsPK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_repetable_transactions",
                table: "repetable_transactions");

            migrationBuilder.AlterColumn<int>(
                name: "pattern_id",
                table: "repetable_transactions",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "repetable_transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0)
                .Annotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_repetable_transactions",
                table: "repetable_transactions",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_repetable_transactions_pattern_id",
                table: "repetable_transactions",
                column: "pattern_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_repetable_transactions",
                table: "repetable_transactions");

            migrationBuilder.DropIndex(
                name: "IX_repetable_transactions_pattern_id",
                table: "repetable_transactions");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "repetable_transactions");

            migrationBuilder.AlterColumn<int>(
                name: "pattern_id",
                table: "repetable_transactions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_repetable_transactions",
                table: "repetable_transactions",
                column: "pattern_id");
        }
    }
}
