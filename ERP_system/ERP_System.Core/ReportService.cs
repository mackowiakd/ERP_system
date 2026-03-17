using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using ERP_System.Core.Enums;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ERP_System.Core
{
    public class ReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db)
        {
            _db = db;
        }

        public byte[] GeneratePdfReport(int requestingUserId, DateTime startDate, DateTime endDate, bool includeHousehold)
        {
            // 1. Get scope (ZMIANA z Users na Employees)
            var requestingUser = _db.Employees.FirstOrDefault(u => u.Id == requestingUserId);
            if (requestingUser == null) return Array.Empty<byte>();

            List<int> userIds = new List<int>();
            string scopeTitle = "Raport Indywidualny";

            if (includeHousehold && requestingUser.CompanyId.HasValue) // ZMIANA z HouseId na CompanyId
            {
                userIds = _db.Employees.Where(u => u.CompanyId == requestingUser.CompanyId).Select(u => u.Id).ToList();
                scopeTitle = "Raport Firmowy";
            }
            else
            {
                userIds.Add(requestingUserId);
            }

            // 2. Fetch Real Transactions (ZMIANA: FinancialOperations, EmployeeId)
            var realTransactions = _db.FinancialOperations
                .Include(t => t.Category)
                .Where(t => userIds.Contains(t.EmployeeId) && t.Date >= startDate && t.Date <= endDate)
                .ToList();

            // 3. Fetch Recurring Rules & Project Virtual Transactions (ZMIANA: Relacja Transaction.Category)
            var recurringRules = _db.RecurringOperations
                .Include(rt => rt.Transaction)
                    .ThenInclude(t => t.Category)
                .Where(rt => rt.Transaction != null && userIds.Contains(rt.Transaction.EmployeeId) && rt.IsActive)
                .ToList();

            var virtualTransactions = new List<DBFinancialOperations>();

            foreach (var rule in recurringRules)
            {
                var iterDate = rule.NextRunDate;

                if (rule.IntervalValue <= 0) continue;

                while (iterDate <= endDate)
                {
                    if (iterDate >= startDate)
                    {
                        virtualTransactions.Add(new DBFinancialOperations
                        {
                            Id = 0, // Virtual
                            CompanyId = rule.Transaction!.CompanyId,
                            EmployeeId = rule.Transaction.EmployeeId,
                            CategoryId = rule.Transaction.CategoryId,
                            Category = rule.Transaction.Category,
                            Value = rule.Transaction.Value,
                            Title = rule.Transaction.Title + " (Plan)",
                            Description = rule.Transaction.Description,
                            Date = iterDate,
                            TransactionType = rule.Transaction.TransactionType,
                            IsRepeatable = false
                        });
                    }
                    iterDate = CalculateNextDate(iterDate, rule.IntervalValue, rule.IntervalType);
                }
            }

            var allTransactions = realTransactions.Concat(virtualTransactions).OrderBy(t => t.Date).ToList();

            // 4. Group data for PDF
            var usersData = userIds.Select(uid => new
            {
                User = _db.Employees.FirstOrDefault(u => u.Id == uid),
                Transactions = allTransactions.Where(t => t.EmployeeId == uid).ToList()
            })
            .Where(d => d.User != null)
            .ToList();

            // 5. Generate PDF
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header()
                        .Text(text =>
                        {
                            text.Span("System ERP - Mini Finanse").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                            text.Span($" - {scopeTitle}").FontSize(16).FontColor(Colors.Grey.Medium);
                        });

                    page.Content()
                        .PaddingVertical(1, Unit.Centimetre)
                        .Column(column =>
                        {
                            column.Item().Text($"Okres: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}").FontSize(12).SemiBold();
                            column.Item().PaddingBottom(10);

                            foreach (var data in usersData)
                            {
                                GenerateUserSection(column, data.User.Login, data.Transactions);
                                column.Item().PageBreak();
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Strona ");
                            x.CurrentPageNumber();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private void GenerateUserSection(ColumnDescriptor column, string username, List<DBFinancialOperations> transactions)
        {
            var pl = new CultureInfo("pl-PL");

            var income = transactions.Where(t => t.Value > 0).Sum(t => t.Value);
            var expense = Math.Abs(transactions.Where(t => t.Value < 0).Sum(t => t.Value));
            var balance = income - expense;

            column.Item().Text($"Wprowadzone przez pracownika: {username}").FontSize(16).Bold().FontColor(Colors.Black);
            column.Item().PaddingBottom(5);

            // Summary Table
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                    columns.RelativeColumn();
                });

                table.Header(header =>
                {
                    header.Cell().Element(CellStyle).Text("Przychody").FontColor(Colors.Green.Medium).SemiBold();
                    header.Cell().Element(CellStyle).Text("Koszty").FontColor(Colors.Red.Medium).SemiBold();
                    header.Cell().Element(CellStyle).Text("Bilans").SemiBold();
                });

                table.Cell().Element(CellStyle).Text(income.ToString("C2", pl));
                table.Cell().Element(CellStyle).Text(expense.ToString("C2", pl));
                table.Cell().Element(CellStyle).Text(balance.ToString("C2", pl));
            });

            column.Item().PaddingBottom(20);

            // Bar Chart Simulation
            column.Item().Text("Wizualizacja").FontSize(14).SemiBold();
            column.Item().PaddingBottom(10);

            var maxValue = Math.Max(income, expense);
            if (maxValue == 0) maxValue = 1;

            column.Item().Row(row =>
            {
                // Income Bar
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Przychód").AlignCenter().FontSize(10);
                    c.Item().Height(150).Column(barCol =>
                    {
                        var ratio = (double)income / (double)maxValue;
                        if (ratio < 0) ratio = 0;
                        if (ratio > 1) ratio = 1;

                        barCol.Item().Height((float)(150 * (1 - ratio)));
                        barCol.Item().Height((float)(150 * ratio)).Background(Colors.Green.Lighten2).Border(1).BorderColor(Colors.Green.Darken2);
                    });
                    c.Item().Text(income.ToString("C0", pl)).AlignCenter().FontSize(9);
                });

                row.Spacing(20);

                // Expense Bar
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text("Koszt").AlignCenter().FontSize(10);
                    c.Item().Height(150).Column(barCol =>
                    {
                        var ratio = (double)expense / (double)maxValue;
                        if (ratio < 0) ratio = 0;
                        if (ratio > 1) ratio = 1;

                        barCol.Item().Height((float)(150 * (1 - ratio)));
                        barCol.Item().Height((float)(150 * ratio)).Background(Colors.Red.Lighten2).Border(1).BorderColor(Colors.Red.Darken2);
                    });
                    c.Item().Text(expense.ToString("C0", pl)).AlignCenter().FontSize(9);
                });

                row.RelativeItem(2);
            });

            column.Item().PaddingBottom(20);

            // Transactions Table
            column.Item().Text("Szczegóły operacji finansowych").FontSize(14).SemiBold();
            column.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(80); // Date
                    columns.RelativeColumn();   // Category
                    columns.RelativeColumn(2);  // Desc
                    columns.ConstantColumn(80); // Amount
                });

                table.Header(header =>
                {
                    header.Cell().Element(HeaderStyle).Text("Data");
                    header.Cell().Element(HeaderStyle).Text("Kategoria");
                    header.Cell().Element(HeaderStyle).Text("Tytuł/Opis");
                    header.Cell().Element(HeaderStyle).Text("Kwota").AlignRight();
                });

                foreach (var transaction in transactions)
                {
                    table.Cell().Element(CellStyle).Text($"{transaction.Date:dd.MM.yyyy}");
                    table.Cell().Element(CellStyle).Text(transaction.Category?.Name ?? "-");
                    table.Cell().Element(CellStyle).Text(transaction.Title ?? "");

                    var color = transaction.Value < 0 ? Colors.Red.Medium : Colors.Green.Medium;

                    table.Cell().Element(CellStyle).Text(transaction.Value.ToString("C2", pl)).FontColor(color).AlignRight();
                }
            });
        }

        static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
        }

        static IContainer HeaderStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingVertical(5).DefaultTextStyle(x => x.SemiBold());
        }

        private DateTime CalculateNextDate(DateTime current, int value, int unit)
        {
            var type = (TransactionIntervalType)unit;
            return type switch
            {
                TransactionIntervalType.Days => current.AddDays(value),
                TransactionIntervalType.Weeks => current.AddDays(value * 7),
                TransactionIntervalType.Months => current.AddMonths(value),
                TransactionIntervalType.Years => current.AddYears(value),
                _ => current.AddMonths(value)
            };
        }
    }
}