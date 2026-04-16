using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERP_System.Web.appMaps
{
    // Handles HTTP requests for report generation and management.
    public class ReportsEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            // GET /reports - Displays the report generator UI.
            app.MapGet("/reports", async (HttpContext context, IWebHostEnvironment env, AppDbContext db) =>
            {
                // Authenticate user from cookie
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Results.Redirect("/login");
                }

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);
                var username = context.Request.Cookies["logged_user"];

                if (user == null)
                {
                    return Results.Redirect("/login");
                }

                var now = DateTime.Now;
                // Pre-populate date inputs for the user's convenience
                var startDate = new DateTime(now.Year, now.Month, 1).ToString("yyyy-MM-dd");
                var endDate = now.ToString("yyyy-MM-dd");

                var filePath = Path.Combine(env.WebRootPath, "reports.html");
                if (!File.Exists(filePath)) 
                    return Results.Content("Błąd: Brak pliku reports.html", "text/plain");

                var html = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                string adminBtnHtml = "";
                
                // Add admin console button if authorized
                if (user.Role == SystemRole.SystemAdmin)
                {
                    adminBtnHtml = "<button class=\"sidebar-link\" onclick=\"window.location.href='/adminConsole'\"><i class=\"fas fa-fw fa-cogs\"></i> &nbsp; Ustawienia Admina</button>";
                }

                // Inject dynamic content into the template
                html = html.Replace("{username}", username)
                           .Replace("{startDate}", startDate)
                           .Replace("{admin_panel_button}", adminBtnHtml)
                           .Replace("{endDate}", endDate);

                return Results.Content(html, "text/html; charset=utf-8");
            });

            // POST /reports/generate/turnoverReport - Generates a Turnover PDF.
            // POST /reports/generate - Generates a Turnover PDF.
            app.MapPost("/reports/generate", (HttpContext context, ReportService reportService) =>
            {
                // 1. Parsowanie userId z ciasteczek
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdString) ||
                    !int.TryParse(userIdString, out int userId))
                {
                    return Results.Redirect("/");
                }

                var form = context.Request.Form;

                // 2. Parsowanie dat (Od - Do)
                if (!DateTime.TryParse(form["startDate"], out DateTime startDate) ||
                    !DateTime.TryParse(form["endDate"], out DateTime endDate))
                {
                    return Results.Content("Nieprawidłowa data");
                }

                // Przesuwamy datę końcową na 23:59:59 (żeby objąć cały ostatni dzień)
                endDate = endDate.Date.AddDays(1).AddTicks(-1);

                // 3. Sprawdzenie zakresu (Firmowy vs Indywidualny)
                var scope = form["reportScope"].ToString();
                bool includeCompany = scope == "company" || scope == "household";

                // 4. Pobieranie nowych parametrów filtracji (Nasza nowa baza danych!)
                string typeFilter = form["transactionTypeFilter"]; // Costs, Revenue, All
                int? contractorId = int.TryParse(form["contractorId"], out int cId) ? cId : null;
                decimal? minAmount = decimal.TryParse(form["minAmount"], out decimal minA) ? minA : null;
                decimal? maxAmount = decimal.TryParse(form["maxAmount"], out decimal maxA) ? maxA : null;

                // 5. Wywołanie naszego serwisu z nowymi filtrami
                var pdfBytes = reportService.GenerateProfitAndLossReport(
                    userId, startDate, endDate, includeCompany, typeFilter, contractorId, minAmount, maxAmount);

                return Results.File(pdfBytes, "application/pdf", $"Zestawienie_Obrotow_{startDate:yyyyMMdd}_{endDate:yyyyMMdd}.pdf");
            });

            // POST /reports/generate/agingReport - Generates an Aging PDF.
            app.MapPost("/reports/generate/agingReport", async (HttpContext context, ReportService reportService) =>
            {
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                {
                    return Results.Redirect("/");
                }

                var form = context.Request.Form;
                // Aging analysis typically uses the end date as the 'as of' reference
                if (!DateTime.TryParse(form["endDate"], out DateTime asOfDate))
                {
                     return Results.Content("Nieprawidłowa data.");
                }
                
                asOfDate = asOfDate.Date.AddDays(1).AddTicks(-1);

                var scope = form["reportScope"].ToString();
                bool includeCompany = scope == "company";

                // Call service to generate the specific aging report
                var pdfBytes = reportService.GenerateAgingReport(userId, asOfDate, includeCompany);
                var filename = $"Raport_Wiekowania_{asOfDate:yyyyMMdd}.pdf";

                return Results.File(pdfBytes, "application/pdf", filename);
            });
        }
    }
}
