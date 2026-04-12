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
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });

                var invoices = invoiceService.GetCompanyInvoices(employee.CompanyId.Value);
                
                var result = invoices.Select(i => new
                {
                    id = i.Id.ToString(),
                    title = $"{i.InvoiceNumber}",
                    startTime = i.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                    endTime = i.DueDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                    amount = i.TotalGross,
                    direction = i.Type,
                    contractorName = (i.Contractor.Name ?? "Nieznany"),
                    adres = i.Contractor.Address,
                    NIP = i.Contractor.TaxId,
                    notes = i.Notes,
                    type = i.Type,
                    status = i.Status,
                    //kontrahent
                    description = "Nazwa Kontrahenta: " + i.Contractor.Name + " | Adres: " + (i.Contractor.Address ?? "Brak ") + " | NIP: " + i.Contractor.TaxId,
                    //finanse
                    description2 = "Typ: " + (i.Type == InvoiceType.Sales ? "Sprzedażowa" : "Kosztowa") + " | Status: " + (i.Status == InvoiceStatus.Paid ? "Opłacona" : "Nieopłacona")
                        + " | Kwota: " + i.TotalGross.ToString() + "zł",
                    description3 = (i.Notes ?? "Brak zawartości"),

                    categoryId = (int?)null,
                    color = i.Type == InvoiceType.Cost ? "#e74a3b" : "#1cc88a",
                    reminder = false,
                });

                return Results.Json(result);
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