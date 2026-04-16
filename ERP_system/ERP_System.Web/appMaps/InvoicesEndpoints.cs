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
            string Notes,
            InvoiceStatus Status
        );
        public record EditInvoiceDto(
            string InvoiceNumber,
            DateTime IssueDate,
            decimal TotalNet, 
            decimal TotalGross, 
            InvoiceType Type, 
            string Notes, 
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
                    return Results.Json(new object[] { }); // Return empty array to prevent JS error

                var invoices = invoiceService.GetCompanyInvoices(employee.CompanyId.Value);
                
                var result = invoices.Select(i => new
                {
                    id = i.Id,
                    invoiceNumber = i.InvoiceNumber,
                    contractorName = i.Contractor?.Name ?? "Nieznany Kontrahent",
                    issueDate = i.IssueDate.ToString("yyyy-MM-dd"),
                    dueDate = i.DueDate.ToString("yyyy-MM-dd"),
                    totalGross = i.TotalGross,
                    type = i.Type.ToString(),
                    status = i.Status.ToString(),
                    notes = i.Notes ?? ""
                });

                return Results.Json(result);
            });

            // NOWY ENDPOINT DLA PULPITU (DASHBOARD) - POBIERA KILKA OSTATNICH FAKTUR JAKO HTML
            app.MapGet("/api/invoices/listSome", async (HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null || employee.CompanyId == null) 
                    return Results.Content("<li class='transaction-item'><div class='transaction-main'><span>Brak autoryzacji.</span></div></li>", "text/html");

                var htmlBuilder = invoiceService.ListInvoicesForDashboard(employee.CompanyId.Value, 5);
                
                return Results.Content(htmlBuilder.ToString(), "text/html");
            });

            // POBIERANIE SZCZEGÓŁÓW JEDNEJ FAKTURY (API)
            app.MapGet("/api/invoices/{id}", async (int id, AppDbContext db) =>
            {
                var invoice = await db.Invoices
                    .Include(i => i.Contractor)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null) return Results.NotFound();

                return Results.Json(new
                {
                    id = invoice.Id,
                    invoiceNumber = invoice.InvoiceNumber,
                    contractorName = invoice.Contractor?.Name ?? "Nieznany Kontrahent",
                    issueDate = invoice.IssueDate.ToString("yyyy-MM-dd"),
                    dueDate = invoice.DueDate.ToString("yyyy-MM-dd"),
                    totalGross = invoice.TotalGross,
                    type = invoice.Type.ToString(),
                    status = invoice.Status.ToString(),
                    notes = invoice.Notes ?? ""
                });
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
                    dto.TotalNet, dto.TotalGross, dto.Type, dto.Notes, dto.Status
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

            // 6. EDYCJA FAKTURY (API)
            app.MapPut("/api/invoices/{id}", async (int id, EditInvoiceDto dto, HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);
                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });
                if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                    return Results.Json(new { success = false, message = "Numer faktury jest wymagany" });
                var result = invoiceService.EditInvoice(id, dto.InvoiceNumber, dto.IssueDate, dto.TotalNet, dto.TotalGross, dto.Type, dto.Notes, dto.Status);
                if (result == "Pomyślnie edytowano fakturę")
                {
                    return Results.Json(new { success = true });
                }
                return Results.Json(new { success = false, message = result });
            });
        }
    }
}