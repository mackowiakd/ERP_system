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

    public record AgingAggregationDto(string ContractorName, string TaxId, decimal TotalDebt, int OverdueCount);
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

            if (minAmount.HasValue) query
                    = query.Where(t => Math.Abs(t.Value) >= minAmount.Value);

            if (maxAmount.HasValue) query =
                query.Where(t => Math.Abs(t.Value) <= maxAmount.Value);

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

        /*RPORT 2 WIEKOWANIE ROZRACHUNKÓW (AGING REPORT)
         * 
         *

        Generates an Aging Report to analyze overdue invoices (Accounts Receivable/Payable aging).
        Parametry wejściowe:

        -Stan zadłużenia na wybrany dzień: Realizowane przez parametr asOfDate. SQL sprawdza faktury, których DueDate (termin płatności) jest mniejszy lub równy tej dacie.
        -Typ dokumentu: Realizowane przez parametr agingType. Filtruje po InvoiceType.Sales (Należności) lub InvoiceType.Cost (Zobowiązania).

        -- Zapytanie dla Raportu 2: Wiekowanie Rozrachunków
            SELECT 
                c.contractor_name AS Name,
                c.contractor_tax_id AS NIP,
                SUM(i.total_gross) AS TotalDebt,
                COUNT(i.invoice_id) AS OverdueCount
            FROM invoices i
            LEFT JOIN contractors c ON i.contractor_id = c.contractor_id
            WHERE i.company_id IN (@ids) 
              AND (i.invoice_status = 0 OR i.invoice_status = 2) -- Unpaid lub PartiallyPaid
              AND i.due_date <= @asOfDate
              AND i.invoice_type = @selectedType -- 0 dla Należności, 1 dla Zobowiązań
            GROUP BY c.contractor_name, c.contractor_tax_id;
         
         */

        public byte[] GenerateAgingReport(int requestingUserId, DateTime asOfDate, bool includeCompany, string agingType = "All")
        {
            var user = _db.Employees.FirstOrDefault(u => u.Id == requestingUserId);
            if (user == null) return Array.Empty<byte>();

            var employeeIds = (includeCompany && user.CompanyId.HasValue)
                ? _db.Employees.Where(e => e.CompanyId == user.CompanyId).Select(e => e.Id).ToList()
                : new List<int> { requestingUserId };

            // 1. ZAPYTANIE BAZOWE: IQueryable - praca na silniku DB
            var query = _db.Invoices.AsQueryable()
                .Where(i => employeeIds.Contains(i.CompanyId)
                         && (i.Status == InvoiceStatus.Unpaid || i.Status == InvoiceStatus.PartiallyPaid)
                         && i.DueDate <= asOfDate);

            // 2. FILTR: Typ dokumentu (Należności / Zobowiązania - specyfikacja tabeli 2)
            if (agingType == "Receivables") query = query.Where(i => i.Type == InvoiceType.Sales);
            else if (agingType == "Payables") query = query.Where(i => i.Type == InvoiceType.Cost);

            // 3. AGREGACJA SQL (GROUP BY względem każdego Kontrahenta)
            var aggregatedData = query
                .GroupBy(i => new {
                    Name = i.Contractor != null ? i.Contractor.Name : "Brak kontrahenta",
                    NIP = i.Contractor != null ? i.Contractor.TaxId : "-"
                })
                .Select(g => new AgingAggregationDto(
                    g.Key.Name,
                    g.Key.NIP,
                    g.Sum(x => x.TotalGross), // SUM()
                    g.Count()                 // COUNT()
                ))
                .ToList(); // Wykonanie na bazie - do RAM trafiają tylko gotowe sumy

            // DUMB TESTING: Obsługa braku danych
            if (!aggregatedData.Any())
            {
                return Document.Create(c => c.Page(p => {
                    p.Margin(2, Unit.Centimetre);
                    p.Content().Text($"Brak spóźnionych płatności na dzień {asOfDate:dd.MM.yyyy}").FontSize(14).Italic();
                })).GeneratePdf();
            }

            // 4. GENEROWANIE PDF
            return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.Header().Text("Raport Wiekowania Rozrachunków").FontSize(20).SemiBold().FontColor(Colors.Blue.Medium);

                    page.Content().PaddingVertical(10).Column(column =>
                    {
                        column.Item().Text($"Stan zadłużenia na dzień: {asOfDate:dd.MM.yyyy}").FontSize(12);
                        column.Item().PaddingTop(10).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Nazwa
                                columns.RelativeColumn(2); // NIP
                                columns.RelativeColumn(1); // Ilość
                                columns.RelativeColumn(2); // Suma
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Kontrahent").SemiBold();
                                header.Cell().Text("NIP").SemiBold();
                                header.Cell().Text("Ilość faktur").SemiBold();
                                header.Cell().Text("Kwota długu").SemiBold();
                            });

                            foreach (var item in aggregatedData)
                            {
                                table.Cell().Text(item.ContractorName);
                                table.Cell().Text(item.TaxId);
                                table.Cell().Text(item.OverdueCount.ToString());
                                table.Cell().Text(item.TotalDebt.ToString("C2", new CultureInfo("pl-PL"))).FontColor(Colors.Red.Medium);
                            }
                        });
                    });
                });
            }).GeneratePdf();
        }

    }
}
