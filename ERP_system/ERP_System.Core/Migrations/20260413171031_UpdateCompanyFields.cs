using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_System.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCompanyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company_name",
                table: "company");

            migrationBuilder.AddColumn<string>(
                name: "company_address",
                table: "company",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company_full_name",
                table: "company",
                type: "TEXT",
                maxLength: 500,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "company_short_name",
                table: "company",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company_address",
                table: "company");

            migrationBuilder.DropColumn(
                name: "company_full_name",
                table: "company");

            migrationBuilder.DropColumn(
                name: "company_short_name",
                table: "company");

            migrationBuilder.AddColumn<string>(
                name: "company_name",
                table: "company",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }
    }
}
