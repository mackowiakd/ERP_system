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
    // Service responsible for generating PDF reports using QuestPDF.
    public class ReportService
    {
        private readonly AppDbContext _db;

        public record CategoryAggregationDto(string Category, decimal TotalSum, int Count);

        public ReportService(AppDbContext db)
        {
            _db = db;
        }

        private record PLReportRow(string CategoryName, decimal TotalSum, int Count);

        public byte[] GenerateProfitAndLossReport(
            int userId,
            DateTime start,
            DateTime end,
            bool includeCompany,
            string typeFilter,
            int? contractorId = null,
            decimal? minAmount = null,
            decimal? maxAmount = null)
        {
            var user = _db.Employees.FirstOrDefault(u => u.Id == userId);
            if (user == null) return Array.Empty<byte>();

            var query = _db.Invoices.Include(i => i.Contractor).AsQueryable();

            // user company filtering 
            if (includeCompany && user.CompanyId.HasValue)
            {
                query = query.Where(i => i.CompanyId == user.CompanyId);
            }
            else
            {
                query = query.Where(i => i.CompanyId == (user.CompanyId ?? 0));
            }

            // IssueDate filtering
            DateTime safeEndDate = end.Date.AddDays(1).AddTicks(-1);
            query = query.Where(i => i.IssueDate >= start.Date && i.IssueDate <= safeEndDate);

            if (contractorId.HasValue && contractorId.Value > 0)
                query = query.Where(i => i.ContractorId == contractorId.Value);

            // Do Zysków i Strat używamy kwot Netto
            if (minAmount.HasValue && minAmount.Value > 0)
                query = query.Where(i => i.TotalNet >= minAmount.Value);

            if (maxAmount.HasValue && maxAmount.Value > 0)
                query = query.Where(i => i.TotalNet <= maxAmount.Value);

            var invoicesFromDb = query.ToList();

            // filtering
            string safeFilter = typeFilter?.ToLower()?.Trim() ?? "";
            bool showRevenues = safeFilter is "wszystko" or "revenue" or "przychody" or "all" or "";
            bool showCosts = safeFilter is "wszystko" or "costs" or "koszty" or "all" or "";

            var revenues = new List<PLReportRow>();
            var costs = new List<PLReportRow>();

            if (showRevenues)
            {
                revenues = invoicesFromDb
                    .Where(i => i.Type == InvoiceType.Sales) // Faktury Sprzedażowe = Przychód
                    .GroupBy(i => i.Contractor != null ? i.Contractor.Name : "Brak przypisanego kontrahenta")
                    .Select(g => new PLReportRow(g.Key, g.Sum(i => i.TotalNet), g.Count()))
                    .OrderByDescending(x => x.TotalSum)
                    .ToList();
            }

            if (showCosts)
            {
                costs = invoicesFromDb
                    .Where(i => i.Type == InvoiceType.Cost) // Faktury Kosztowe = Wydatki
                    .GroupBy(i => i.Contractor != null ? i.Contractor.Name : "Brak przypisanego kontrahenta")
                    .Select(g => new PLReportRow(g.Key, g.Sum(i => i.TotalNet), g.Count()))
                    .OrderByDescending(x => x.TotalSum)
                    .ToList();
            }

            // Generation of PDF document with Quest PDF
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
                            col.Item().Text("Zestawienie Obrotów (Zyski i Straty)").FontSize(14).FontColor(Colors.Grey.Medium);
                        });
                        row.RelativeItem().AlignRight().Column(col => 
                        {
                            col.Item().Text($"Okres: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}").AlignRight();
                            col.Item().Text($"Filtr typu: {typeFilter}").AlignRight().FontColor(Colors.Grey.Medium);
                        });
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        var pl = new CultureInfo("pl-PL");

                        void RenderCategoryTable(string title, IEnumerable<PLReportRow> data, string colorHex)
                        {
                            var dataList = data.ToList();
                            
                            column.Item().PaddingTop(15).PaddingBottom(5).Text(title).FontSize(14).Bold().FontColor(colorHex);

                            if (!dataList.Any())
                            {
                                column.Item().Text("Brak operacji w tej kategorii dla wybranego okresu.").Italic().FontColor(Colors.Grey.Darken1);
                                return;
                            }

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3); 
                                    columns.ConstantColumn(100); 
                                    columns.ConstantColumn(120); 
                                });

                                table.Header(header =>
                                {
                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text("Kontrahent / Źródło");
                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text("Ilość faktur").AlignRight();
                                    header.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5).Text("Kwota Netto").AlignRight();
                                });

                                foreach (var item in dataList)
                                {
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5).Text(item.CategoryName);
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5).Text(item.Count.ToString()).AlignRight();
                                    table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5).Text(item.TotalSum.ToString("C2", pl)).AlignRight();
                                }
                                
                                table.Cell().ColumnSpan(2).PaddingTop(5).Text("SUMA:").AlignRight().SemiBold();
                                table.Cell().PaddingTop(5).Text(dataList.Sum(x => x.TotalSum).ToString("C2", pl)).AlignRight().SemiBold();
                            });
                        }

                        if (showRevenues) RenderCategoryTable("PRZYCHODY (Zafakturowana Sprzedaż)", revenues, Colors.Green.Darken2);
                        if (showCosts) RenderCategoryTable("KOSZTY (Faktury Kosztowe)", costs, Colors.Red.Darken2);

                        if (showRevenues && showCosts)
                        {
                            decimal totalRevenues = revenues.Sum(r => r.TotalSum);
                            decimal totalCosts = costs.Sum(c => c.TotalSum);
                            decimal netProfit = totalRevenues - totalCosts; 

                            string profitColor = netProfit >= 0 ? Colors.Green.Darken2 : Colors.Red.Darken2;
                            string profitLabel = netProfit >= 0 ? "ZYSK NETTO W OKRESIE:" : "STRATA NETTO W OKRESIE:";

                            column.Item().PaddingTop(30).Background(Colors.Grey.Lighten4).Padding(10).Row(row => 
                            {
                                row.RelativeItem().Text(profitLabel).FontSize(16).SemiBold();
                                row.RelativeItem().AlignRight().Text(netProfit.ToString("C2", pl)).FontSize(16).Bold().FontColor(profitColor);
                            });
                        }

                        if (!revenues.Any() && !costs.Any())
                        {
                            column.Item().PaddingVertical(20).AlignCenter().Text("Brak faktur spełniających kryteria raportu.");
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

        private byte[] CreatePdf(IEnumerable<CategoryAggregationDto> data, DateTime start, DateTime end)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, Unit.Centimetre);
                    page.Header().Text("Zestawienie Obrotów (Zyski i Straty)").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                    page.Content().PaddingVertical(10).Column(col =>
                    {
                        col.Item().Text($"Okres: {start:dd.MM.yyyy} - {end:dd.MM.yyyy}");
                        col.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Kategoria").SemiBold();
                                header.Cell().Text("Ilość").SemiBold();
                                header.Cell().Text("Suma").SemiBold();
                            });

                            foreach (var item in data)
                            {
                                
                                table.Cell().Text(item.Category);
                                table.Cell().Text(item.Count.ToString());
                                table.Cell().Text(item.TotalSum.ToString("C2", new CultureInfo("pl-PL")));
                            }
                        });
                    });
                });
            });

            return document.GeneratePdf();
        }

        /*RAPORT 2

        Generates an Aging Report to analyze overdue invoices (Accounts Receivable/Payable aging).
        */
        // Auxiliary record to avoid the 'dynamic' type and problems with QuestPDF
        private record AgingReportRow(string ContractorName, string ContractorNip, int InvoiceCount, decimal TotalDebt);

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

            var unpaidInvoices = query
                .Where(i => (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid) 
                        && i.IssueDate <= asOfDate)
                .ToList();

            var receivables = unpaidInvoices
                .Where(i => i.Type == InvoiceType.Sales)
                .GroupBy(i => i.Contractor)
                .Select(g => new AgingReportRow(
                    g.Key?.Name ?? "Nieznany",
                    g.Key?.TaxId ?? "Brak NIP",
                    g.Count(),
                    g.Sum(i => i.TotalGross)
                ))
                .OrderByDescending(x => x.TotalDebt)
                .ToList();

            var payables = unpaidInvoices
                .Where(i => i.Type == InvoiceType.Cost)
                .GroupBy(i => i.Contractor)
                .Select(g => new AgingReportRow(
                    g.Key?.Name ?? "Nieznany",
                    g.Key?.TaxId ?? "Brak NIP", 
                    g.Count(),
                    g.Sum(i => i.TotalGross)
                ))
                .OrderByDescending(x => x.TotalDebt)
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
                        row.RelativeItem().AlignRight().Text($"Stan zadłużenia na dzień: {asOfDate:dd.MM.yyyy}");
                    });

                    page.Content().PaddingVertical(1, Unit.Centimetre).Column(column =>
                    {
                        var pl = new CultureInfo("pl-PL");

                        void RenderSection(string title, IEnumerable<AgingReportRow> data)
                        {
                            var dataList = data.ToList();
                            
                            column.Item().PaddingTop(15).PaddingBottom(5).Text(title).FontSize(14).Bold().FontColor(Colors.Blue.Darken2);

                            if (!dataList.Any())
                            {
                                column.Item().Text("Brak rozrachunków w tej kategorii.").Italic().FontColor(Colors.Grey.Darken1);
                                return;
                            }

                            column.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);  
                                    columns.RelativeColumn(1);  
                                    columns.ConstantColumn(80); 
                                    columns.ConstantColumn(100); 
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Element(HeaderStyle).Text("Nazwa Kontrahenta");
                                    header.Cell().Element(HeaderStyle).Text("NIP");
                                    header.Cell().Element(HeaderStyle).Text("Ilość faktur").AlignRight();
                                    header.Cell().Element(HeaderStyle).Text("Kwota długu").AlignRight();
                                });

                                foreach (var item in dataList)
                                {
                                    table.Cell().Element(CellStyle).Text(item.ContractorName);
                                    table.Cell().Element(CellStyle).Text(item.ContractorNip);
                                    table.Cell().Element(CellStyle).Text(item.InvoiceCount.ToString()).AlignRight();
                                    table.Cell().Element(CellStyle).Text(item.TotalDebt.ToString("C2", pl)).AlignRight();
                                }
                                
                                table.Cell().ColumnSpan(3).Element(FooterStyle).Text("SUMA:").AlignRight().SemiBold();
                                table.Cell().Element(FooterStyle).Text(dataList.Sum(x => x.TotalDebt).ToString("C2", pl)).AlignRight().SemiBold();
                            });
                        }

                        RenderSection("Należności od klientów (Faktury Sprzedażowe)", receivables);
                        RenderSection("Zobowiązania firmy (Faktury Kosztowe)", payables);

                        if (!receivables.Any() && !payables.Any())
                        {
                            column.Item().PaddingVertical(20).AlignCenter().Text("Brak zaległych płatności spełniających kryteria na wybrany dzień.");
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
            
            static IContainer HeaderStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten2).PaddingVertical(5);
            static IContainer CellStyle(IContainer container) => container.BorderBottom(1).BorderColor(Colors.Grey.Lighten3).PaddingVertical(5);
            static IContainer FooterStyle(IContainer container) => container.PaddingTop(5);
        }
    }
}
