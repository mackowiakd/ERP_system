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


        /* RPORT 1
         *
         *
        -- ========================================================================
        -- RAPORT 1: Zestawienie Obrotów (Zyski i Straty) z agregacją w bazie
        -- ========================================================================

    SELECT 
        COALESCE(c.Name, 'Brak kategorii') AS CategoryName,
        SUM(f.Value) AS TotalSum,
        COUNT(f.Id) AS OperationCount

    FROM FinancialOperations f
    LEFT JOIN TransactionCategories c ON f.CategoryId = c.Id

    WHERE 
        -- 1. Filtr bezpieczeństwa i uprawnień (kto należy do firmy)
        f.EmployeeId IN (@emp1, @emp2, @emp3) 
    
        -- 2. Sztywne ramy czasowe
        AND f.Date >= @startDate 
        AND f.Date <= @endDate
    
        -- ==========================================================
        -- PONIŻSZE WARUNKI SĄ DYNAMICZNE (wstawiane przez EF Core
        -- w zależności od tego, co użytkownik wybrał w formularzu):
        -- ==========================================================
    
        -- a) Jeśli wybrano "Tylko Koszty" (Costs):
        AND f.Value < 0
    
        -- b) Jeśli wybrano Konkretnego Kontrahenta:
        AND f.ContractorId = @contractorId
    
        -- c) Jeśli wybrano przedział kwotowy:
        AND ABS(f.Value) >= @minAmount
        AND ABS(f.Value) <= @maxAmount

    -- 3. Kluczowy punkt: Grupowanie po kategorii (zabezpieczone przed NULL)
    GROUP BY 
        COALESCE(c.Name, 'Brak kategorii');
        -- ========================================================================

        Zakres dat, Typ operacji (Tylko Koszty / Tylko Przychody / Wszystko)
         * 
         * Zestawienie Obrotów (Zyski i Straty)
         */


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
                /* * SQL: 
                 * SELECT * FROM Employees 
                 * WHERE Id = @userId 
                 * LIMIT 1; 
                 */
                var user = _db.Employees.FirstOrDefault(u => u.Id == userId);
                if (user == null) return Array.Empty<byte>();

                // 1. Definiujemy zakres pracowników
                /* * SQL (Jeśli includeCompany == true): 
                 * SELECT Id FROM Employees 
                 * WHERE CompanyId = @companyId; 
                 */
                var employeeIds = (includeCompany && user.CompanyId.HasValue)
                    ? _db.Employees.Where(e => e.CompanyId == user.CompanyId).Select(e => e.Id).ToList()
                    : new List<int> { userId };

                // 2. Budujemy zapytanie (IQueryable - na tym etapie NIE wysyłamy jeszcze nic do bazy!)
                var query = _db.FinancialOperations.AsQueryable();

                // DYNAMICZNA FILTRACJA (Entity Framework buduje klauzulę WHERE w pamięci)
                query = query.Where(t => employeeIds.Contains(t.EmployeeId) && t.Date >= start && t.Date <= end);

                if (typeFilter == "Costs") query = query.Where(t => t.Value < 0);
                else if (typeFilter == "Revenue") query = query.Where(t => t.Value > 0);

            if (contractorId.HasValue) query = 
                    query.Where(t => t.Invoice != null && t.Invoice.ContractorId == contractorId.Value);

            if (minAmount.HasValue) 
                    query= query.Where(t => t.Value >= minAmount.Value || t.Value <= -minAmount.Value );

            if (maxAmount.HasValue) 
                    query = query.Where(t => t.Value<= maxAmount.Value && t.Value >= -maxAmount.Value);

            // 3. AGREGACJA PO STRONIE BAZY (GROUP BY)
            /* * =================================================================================
             * GŁÓWNE ZAPYTANIE SQL WYSYŁANE DO BAZY (Wykonuje się w momencie wywołania .ToList()):
             * * SELECT 
             * COALESCE(c.Name, 'Brak kategorii') AS Category, 
             * SUM(f.Value) AS TotalSum, 
             * COUNT(*) AS Count
             * FROM FinancialOperations f
             * LEFT JOIN TransactionCategories c ON f.CategoryId = c.Id
             * WHERE f.EmployeeId IN (@emp1, @emp2, ...) 
             * AND f.Date >= @start AND f.Date <= @end
             * AND f.Value < 0 -- (dodane dynamicznie np. dla typeFilter == "Costs")
             * GROUP BY 
             * COALESCE(c.Name, 'Brak kategorii');
             * =================================================================================
             */
               var aggregatedData = query
                 .GroupBy(t => t.Category != null ? t.Category.Name : "Brak kategorii")
                 .Select(g => new CategoryAggregationDto( // <--- ZMIANA: Używamy naszego rekordu
                     g.Key,
                     g.Sum(x => x.Value),
                     g.Count()
                 ))
                 .ToList();

            // 4. Generowanie PDF na podstawie ZAGREGOWANYCH danych z bazy
            return CreatePdf(aggregatedData, start, end);
        }

            private byte[] CreatePdf(IEnumerable<CategoryAggregationDto> data, DateTime start, DateTime end)
            {
                // (Logika generowania pliku PDF zostaje bez zmian)
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
     
    /*RPORT 2

    Generates an Aging Report to analyze overdue invoices (Accounts Receivable/Payable aging).
    */
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
