using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Web.appMaps
{
    public class InvoicesEndpoints : IEndpoint
    {
        // Struktura danych z formularza HTML, używa Waszych enumów z DBTables!
        public record CreateInvoiceDto(
            int ContractorId, 
            string InvoiceNumber, 
            DateTime IssueDate, 
            DateTime DueDate, 
            PaymentMethod PaymentMethod, 
            decimal TotalNet, 
            decimal TotalGross, 
            InvoiceType Type, 
            InvoiceStatus Status
        );

        public void Map(IEndpointRouteBuilder app)
        {
            // 1. ZWRACANIE GŁÓWNEGO WIDOKU LISTY FAKTUR (Stworzymy go za chwilę)
            app.MapGet("/invoices", async (HttpContext context, AppDbContext db) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (user == null) { context.Response.Redirect("/login"); return Results.Empty; }

                var html = await System.IO.File.ReadAllTextAsync("wwwroot/invoices.html");
                html = html.Replace("{username}", user.Login);
                
                if (user.Role.ToString() == "Admin") {
                    html = html.Replace("{admin_panel_button}", "<button class=\"sidebar-link\" onclick=\"window.location.href='/admin'\"><i class=\"fas fa-cog\"></i> &nbsp; Panel Firmy</button>");
                } else {
                    html = html.Replace("{admin_panel_button}", "");
                }

                return Results.Content(html, "text/html");
            });
            
            // 3. POBIERANIE LISTY FAKTUR (API)
            app.MapGet("/api/invoices", async (HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });

                var invoices = invoiceService.GetCompanyInvoices(employee.CompanyId.Value);
                
                var result = invoices.Select(i => new
                {
                    id = i.Id,
                    invoiceNumber = i.InvoiceNumber,
                    contractorName = i.Contractor?.Name ?? "Nieznany Kontrahent", // Wyciągamy nazwę firmy!
                    issueDate = i.IssueDate.ToString("yyyy-MM-dd"),
                    dueDate = i.DueDate.ToString("yyyy-MM-dd"),
                    totalGross = i.TotalGross,
                    type = i.Type.ToString(),
                    status = i.Status.ToString()
                });

                return Results.Json(result);
            });

            // loads a few of the newest invoices for dashboard
            app.MapGet("/api/invoices/listSome", async (HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null || employee.CompanyId == null) 
                    return Results.Content("<li class='transaction-item'><div class='transaction-main'><span>Brak autoryzacji.</span></div></li>", "text/html");

                var htmlBuilder = invoiceService.ListInvoicesForDashboard(employee.CompanyId.Value, 5);
                
                return Results.Content(htmlBuilder.ToString(), "text/html");
            });

            // 4. DODAWANIE NOWEJ FAKTURY (API)
            app.MapPost("/api/invoices", async (CreateInvoiceDto dto, HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });

                if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                    return Results.Json(new { success = false, message = "Numer faktury jest wymagany" });

                var result = invoiceService.AddInvoice(
                    employee.CompanyId.Value, dto.ContractorId, dto.InvoiceNumber, 
                    dto.IssueDate, dto.DueDate, dto.PaymentMethod, 
                    dto.TotalNet, dto.TotalGross, dto.Type, dto.Status
                );

                if (result == "Pomyślnie dodano fakturę")
                {
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, message = result });
            });

            // 5. USUWANIE FAKTURY (API)
            app.MapDelete("/api/invoices/{id}", async (int id, HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });

                var result = invoiceService.DeleteInvoice(id, employee.CompanyId.Value);

                if (result == "Pomyślnie usunięto fakturę")
                {
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, message = result });
            });
        }
    }
}