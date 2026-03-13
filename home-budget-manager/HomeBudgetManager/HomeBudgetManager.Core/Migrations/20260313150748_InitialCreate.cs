using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HomeBudgetManager.Core.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    role_id = table.Column<int>(type: "INTEGER", nullable: false),
                    role_name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.role_id);
                });

            migrationBuilder.CreateTable(
                name: "company",
                columns: table => new
                {
                    company_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    company_admin_id = table.Column<int>(type: "INTEGER", nullable: false),
                    company_name = table.Column<string>(type: "TEXT", nullable: false),
                    company_description = table.Column<string>(type: "TEXT", nullable: true),
                    company_join_code = table.Column<string>(type: "TEXT", nullable: false),
                    company_nip = table.Column<string>(type: "TEXT", maxLength: 15, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_company", x => x.company_id);
                });

            migrationBuilder.CreateTable(
                name: "contractors",
                columns: table => new
                {
                    contractor_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    company_id = table.Column<int>(type: "INTEGER", nullable: false),
                    contractor_name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    contractor_tax_id = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contractors", x => x.contractor_id);
                    table.ForeignKey(
                        name: "FK_contractors_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                columns: table => new
                {
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    employee_email = table.Column<string>(type: "TEXT", nullable: false),
                    employee_login = table.Column<string>(type: "TEXT", nullable: false),
                    employee_password = table.Column<string>(type: "TEXT", nullable: false),
                    employee_role = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_company_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.employee_id);
                    table.ForeignKey(
                        name: "FK_Employees_company_employee_company_id",
                        column: x => x.employee_company_id,
                        principalTable: "company",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "invoices",
                columns: table => new
                {
                    invoice_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    company_id = table.Column<int>(type: "INTEGER", nullable: false),
                    contractor_id = table.Column<int>(type: "INTEGER", nullable: false),
                    invoice_number = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    issue_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    due_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    payment_method = table.Column<int>(type: "INTEGER", nullable: false),
                    total_net = table.Column<decimal>(type: "TEXT", nullable: false),
                    total_gross = table.Column<decimal>(type: "TEXT", nullable: false),
                    invoice_type = table.Column<int>(type: "INTEGER", nullable: false),
                    invoice_status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_invoices", x => x.invoice_id);
                    table.ForeignKey(
                        name: "FK_invoices_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_invoices_contractors_contractor_id",
                        column: x => x.contractor_id,
                        principalTable: "contractors",
                        principalColumn: "contractor_id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "transaction_categories",
                columns: table => new
                {
                    category_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    company_id = table.Column<int>(type: "INTEGER", nullable: true),
                    category_name = table.Column<string>(type: "TEXT", nullable: false),
                    category_description = table.Column<string>(type: "TEXT", nullable: true),
                    DBEmployeeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transaction_categories", x => x.category_id);
                    table.ForeignKey(
                        name: "FK_transaction_categories_Employees_DBEmployeeId",
                        column: x => x.DBEmployeeId,
                        principalTable: "Employees",
                        principalColumn: "employee_id");
                    table.ForeignKey(
                        name: "FK_transaction_categories_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "company_id");
                });

            migrationBuilder.CreateTable(
                name: "DBContractorDBInvoice",
                columns: table => new
                {
                    InvoicesId = table.Column<int>(type: "INTEGER", nullable: false),
                    RelatedContractorInvoicesId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DBContractorDBInvoice", x => new { x.InvoicesId, x.RelatedContractorInvoicesId });
                    table.ForeignKey(
                        name: "FK_DBContractorDBInvoice_contractors_RelatedContractorInvoicesId",
                        column: x => x.RelatedContractorInvoicesId,
                        principalTable: "contractors",
                        principalColumn: "contractor_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DBContractorDBInvoice_invoices_InvoicesId",
                        column: x => x.InvoicesId,
                        principalTable: "invoices",
                        principalColumn: "invoice_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FinancialOperations",
                columns: table => new
                {
                    transaction_id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    category_id = table.Column<int>(type: "INTEGER", nullable: false),
                    company_id = table.Column<int>(type: "INTEGER", nullable: false),
                    employee_id = table.Column<int>(type: "INTEGER", nullable: false),
                    transaction_value = table.Column<decimal>(type: "TEXT", nullable: false),
                    transaction_type = table.Column<int>(type: "INTEGER", nullable: false),
                    transaction_title = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    transaction_description = table.Column<string>(type: "TEXT", nullable: true),
                    transaction_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    transaction_is_repeatable = table.Column<bool>(type: "INTEGER", nullable: false),
                    invoice_id = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FinancialOperations", x => x.transaction_id);
                    table.ForeignKey(
                        name: "FK_FinancialOperations_Employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "Employees",
                        principalColumn: "employee_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinancialOperations_company_company_id",
                        column: x => x.company_id,
                        principalTable: "company",
                        principalColumn: "company_id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_FinancialOperations_invoices_invoice_id",
                        column: x => x.invoice_id,
                        principalTable: "invoices",
                        principalColumn: "invoice_id");
                    table.ForeignKey(
                        name: "FK_FinancialOperations_transaction_categories_category_id",
                        column: x => x.category_id,
                        principalTable: "transaction_categories",
                        principalColumn: "category_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "repetable_transactions",
                columns: table => new
                {
                    pattern_id = table.Column<int>(type: "INTEGER", nullable: false),
                    interval_value = table.Column<int>(type: "INTEGER", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    next_run_date = table.Column<DateTime>(type: "TEXT", nullable: false),
                    IntervalType = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repetable_transactions", x => x.pattern_id);
                    table.ForeignKey(
                        name: "FK_repetable_transactions_FinancialOperations_pattern_id",
                        column: x => x.pattern_id,
                        principalTable: "FinancialOperations",
                        principalColumn: "transaction_id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "role_id", "role_name" },
                values: new object[,]
                {
                    { 0, "Guest" },
                    { 1, "Employee" },
                    { 2, "Accountant" },
                    { 3, "CompanyAdmin" },
                    { 4, "SystemAdmin" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_company_company_admin_id",
                table: "company",
                column: "company_admin_id");

            migrationBuilder.CreateIndex(
                name: "IX_contractors_company_id",
                table: "contractors",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_DBContractorDBInvoice_RelatedContractorInvoicesId",
                table: "DBContractorDBInvoice",
                column: "RelatedContractorInvoicesId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_employee_company_id",
                table: "Employees",
                column: "employee_company_id");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_employee_email",
                table: "Employees",
                column: "employee_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOperations_category_id",
                table: "FinancialOperations",
                column: "category_id");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOperations_company_id",
                table: "FinancialOperations",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOperations_employee_id",
                table: "FinancialOperations",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_FinancialOperations_invoice_id",
                table: "FinancialOperations",
                column: "invoice_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_company_id",
                table: "invoices",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_invoices_contractor_id",
                table: "invoices",
                column: "contractor_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_categories_company_id",
                table: "transaction_categories",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_transaction_categories_DBEmployeeId",
                table: "transaction_categories",
                column: "DBEmployeeId");

            migrationBuilder.AddForeignKey(
                name: "FK_company_Employees_company_admin_id",
                table: "company",
                column: "company_admin_id",
                principalTable: "Employees",
                principalColumn: "employee_id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_company_Employees_company_admin_id",
                table: "company");

            migrationBuilder.DropTable(
                name: "DBContractorDBInvoice");

            migrationBuilder.DropTable(
                name: "repetable_transactions");

            migrationBuilder.DropTable(
                name: "roles");

            migrationBuilder.DropTable(
                name: "FinancialOperations");

            migrationBuilder.DropTable(
                name: "invoices");

            migrationBuilder.DropTable(
                name: "transaction_categories");

            migrationBuilder.DropTable(
                name: "contractors");

            migrationBuilder.DropTable(
                name: "Employees");

            migrationBuilder.DropTable(
                name: "company");
        }
    }
}
