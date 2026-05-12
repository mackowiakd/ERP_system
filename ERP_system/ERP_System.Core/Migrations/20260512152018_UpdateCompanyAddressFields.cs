using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ERP_System.Core.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCompanyAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "company",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Street",
                table: "company",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ZipCode",
                table: "company",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "company");

            migrationBuilder.DropColumn(
                name: "Street",
                table: "company");

            migrationBuilder.DropColumn(
                name: "ZipCode",
                table: "company");
        }
    }
}
