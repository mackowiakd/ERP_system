using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ERP_System.Core
{
    /// <summary>
    /// Service responsible for generating PDF reports using QuestPDF.
    /// Provides methods for Turnover and Aging analysis based on Invoices.
    /// </summary>
    public class ReportService
    {
        private readonly AppDbContext _db;

        public ReportService(AppDbContext db)
        {
            _db = db;
        }

        /// <summary>
        /// Generates a Turnover Report (Sales vs Costs) for a specified period.
        /// </summary>
        /// <param name="requestingUserId">The ID of the employee requesting the report.</param>
        /// <param name="startDate">Beginning of the report period.</param>
        /// <param name="endDate">End of the report period.</param>
        /// <param name="includeCompany">Whether to include all company invoices or just individual ones (if applicable).</param>
        /// <returns>PDF byte array.</returns>
        public byte[] GenerateTurnoverReport(int requestingUserId, DateTime startDate, DateTime endDate, bool includeCompany)
        {
            // Retrieve the requesting user to determine their company context
            var user = _db.Employees.FirstOrDefault(u => u.Id == requestingUserId);
            if (user == null) return Array.Empty<byte>();

            // Base query for invoices including contractor details for the report
            IQueryable<DBInvoice> query = _db.Invoices.Include(i => i.Contractor);
            string scopeTitle = "Raport Zestawienie Obrotów - Indywidualny";

            // If company scope is selected, fetch all invoices for the user's company
            if (includeCompany && user.CompanyId.HasValue)
            {
                query = query.Where(i => i.CompanyId == user.CompanyId);
                scopeTitle = "Raport Zestawienie Obrotów - Firmowy";
            }
            else
            {
                // In this system, invoices are primarily company-wide, but we filter by the user's company ID
                query = query.Where(i => i.CompanyId == (user.CompanyId ?? 0));
            }

            // Filter invoices by the provided date range
            var invoices = query.Where(i => i.IssueDate >= startDate && i.IssueDate <= endDate)
                                .OrderBy(i => i.IssueDate)
                                .ToList();

            // Create the PDF document structure
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    // Header section with system name and report type
                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Mini-ERP System").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                            col.Item().Text(scopeTitle).FontSize(14).FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                    });

                    // Main content section
                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().Text($"Okres: {startDate:dd.MM.yyyy} - {endDate:dd.MM.yyyy}").FontSize(12).SemiBold();
                        column.Item().PaddingBottom(10);

                        var pl = new CultureInfo("pl-PL");
                        // Calculate total sales and costs
                        var sales = invoices.Where(i => i.Type == InvoiceType.Sales).Sum(i => i.TotalGross);
                        var costs = invoices.Where(i => i.Type == InvoiceType.Cost).Sum(i => i.TotalGross);
                        var balance = sales - costs;

                        // Summary table for key metrics
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
                                header.Cell().Element(CellStyle).Text("Suma Sprzedaży").FontColor(Colors.Green.Medium).SemiBold();
                                header.Cell().Element(CellStyle).Text("Suma Kosztów").FontColor(Colors.Red.Medium).SemiBold();
                                header.Cell().Element(CellStyle).Text("Wynik (Bilans)").SemiBold();
                            });

                            table.Cell().Element(CellStyle).Text(sales.ToString("C2", pl));
                            table.Cell().Element(CellStyle).Text(costs.ToString("C2", pl));
                            table.Cell().Element(CellStyle).Text(balance.ToString("C2", pl));
                        });

                        column.Item().PaddingBottom(20);

                        // Detailed list of invoices
                        column.Item().Text("Szczegóły operacji").FontSize(14).SemiBold();
                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(80); // Date
                                columns.RelativeColumn(2);  // Contractor
                                columns.RelativeColumn(2);  // Number
                                columns.ConstantColumn(80); // Amount
                            });

                            table.Header(header =>
                            {
                                header.Cell().Element(HeaderStyle).Text("Data");
                                header.Cell().Element(HeaderStyle).Text("Kontrahent");
                                header.Cell().Element(HeaderStyle).Text("Nr Faktury");
                                header.Cell().Element(HeaderStyle).Text("Kwota Brutto").AlignRight();
                            });

                            foreach (var inv in invoices)
                            {
                                table.Cell().Element(CellStyle).Text($"{inv.IssueDate:dd.MM.yyyy}");
                                table.Cell().Element(CellStyle).Text(inv.Contractor?.Name ?? "Nieznany");
                                table.Cell().Element(CellStyle).Text(inv.InvoiceNumber);

                                // Color code amount based on invoice type
                                var color = inv.Type == InvoiceType.Cost ? Colors.Red.Medium : Colors.Green.Medium;
                                table.Cell().Element(CellStyle).Text(inv.TotalGross.ToString("C2", pl)).FontColor(color).AlignRight();
                            }
                        });
                    });

                    // Footer with page numbering
                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Strona ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        /// <summary>
        /// Generates an Aging Report to analyze overdue invoices (Accounts Receivable/Payable aging).
        /// </summary>
        /// <param name="requestingUserId">ID of the requesting employee.</param>
        /// <param name="asOfDate">The reference date to calculate delays.</param>
        /// <param name="includeCompany">Whether to analyze for the entire company.</param>
        /// <returns>PDF byte array.</returns>
        public byte[] GenerateAgingReport(int requestingUserId, DateTime asOfDate, bool includeCompany)
        {
            var user = _db.Employees.FirstOrDefault(u => u.Id == requestingUserId);
            if (user == null) return Array.Empty<byte>();

            IQueryable<DBInvoice> query = _db.Invoices.Include(i => i.Contractor);
            string scopeTitle = "Raport Wiekowania Rozrachunków - Indywidualny";

            if (includeCompany && user.CompanyId.HasValue)
            {
                query = query.Where(i => i.CompanyId == user.CompanyId);
                scopeTitle = "Raport Wiekowania Rozrachunków - Firmowy";
            }
            else
            {
                query = query.Where(i => i.CompanyId == (user.CompanyId ?? 0));
            }

            // Fetch invoices that are not fully paid
            var unpaidInvoices = query.Where(i => (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid) && i.IssueDate <= asOfDate)
                                      .ToList();

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                    page.Header().Row(row =>
                    {
                        row.RelativeItem().Column(col =>
                        {
                            col.Item().Text("Mini-ERP System").SemiBold().FontSize(20).FontColor(Colors.Blue.Medium);
                            col.Item().Text(scopeTitle).FontSize(14).FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Text($"Stan na dzień: {asOfDate:dd.MM.yyyy}");
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        column.Item().PaddingBottom(10).Text("Analiza przeterminowanych płatności").FontSize(14).SemiBold();

                        var pl = new CultureInfo("pl-PL");

                        // Define standard aging buckets for grouping
                        var agingBuckets = new[]
                        {
                            new { Name = "Niewymagalne (przed terminem)", Min = int.MinValue, Max = 0 },
                            new { Name = "1 - 30 dni po terminie", Min = 1, Max = 30 },
                            new { Name = "31 - 60 dni po terminie", Min = 31, Max = 60 },
                            new { Name = "61 - 90 dni po terminie", Min = 61, Max = 90 },
                            new { Name = "Powyżej 90 dni", Min = 91, Max = int.MaxValue }
                        };

                        foreach (var bucket in agingBuckets)
                        {
                            var filtered = unpaidInvoices.Where(i =>
                            {
                                var daysLate = (asOfDate - i.DueDate).Days;
                                return daysLate >= bucket.Min && daysLate <= bucket.Max;
                            }).ToList();

                            if (!filtered.Any()) continue;

                            column.Item().PaddingTop(10).Text(bucket.Name).FontSize(12).Bold().FontColor(Colors.Blue.Darken2);

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(2);  // Contractor
                                    columns.RelativeColumn(2);  // Invoice Number
                                    columns.ConstantColumn(80); // Due Date
                                    columns.ConstantColumn(50); // Days
                                    columns.ConstantColumn(80); // Amount
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Kontrahent");
                                    header.Cell().Element(HeaderStyle).Text("Nr Faktury");
                                    header.Cell().Element(HeaderStyle).Text("Termin");
                                    header.Cell().Element(HeaderStyle).Text("Dni").AlignRight();
                                    header.Cell().Element(HeaderStyle).Text("Kwota").AlignRight();
                                });

                                foreach (var inv in filtered)
                                {
                                    var daysLate = (asOfDate - inv.DueDate).Days;
                                    table.Cell().Element(CellStyle).Text(inv.Contractor?.Name ?? "Nieznany");
                                    table.Cell().Element(CellStyle).Text(inv.InvoiceNumber);
                                    table.Cell().Element(CellStyle).Text($"{inv.DueDate:dd.MM.yyyy}");
                                    table.Cell().Element(CellStyle).Text(daysLate > 0 ? daysLate.ToString() : "0").AlignRight();
                                    table.Cell().Element(CellStyle).Text(inv.TotalGross.ToString("C2", pl)).AlignRight();
                                }
                            });
                        }

                        if (!unpaidInvoices.Any())
                        {
                            column.Item().PaddingVertical(20).AlignCenter().Text("Brak nieopłaconych faktur spełniających kryteria.");
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Strona ");
                        x.CurrentPageNumber();
                    });
                });
            });

            return document.GeneratePdf();
        }

        // Helper style methods for table layout consistency
        private static IContainer CellStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
        }

        private static IContainer HeaderStyle(IContainer container)
        {
            return container.BorderBottom(1).BorderColor(Colors.Grey.Medium).PaddingVertical(5).DefaultTextStyle(x => x.SemiBold());
        }
    }
}
