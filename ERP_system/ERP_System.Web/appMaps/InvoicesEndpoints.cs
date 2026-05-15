using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.Text.Json;

namespace ERP_System.Web.appMaps
{
    public class InvoicesEndpoints : IEndpoint
    {
        public class CreateInvoiceDto
        {
            [JsonPropertyName("contractorId")] public int ContractorId { get; set; }
            [JsonPropertyName("invoiceNumber")] public string InvoiceNumber { get; set; } = "";
            [JsonPropertyName("issueDate")] public DateTime IssueDate { get; set; }
            [JsonPropertyName("dueDate")] public DateTime DueDate { get; set; }
            [JsonPropertyName("paymentMethod")] public int PaymentMethod { get; set; }
            [JsonPropertyName("totalNet")] public decimal TotalNet { get; set; }
            [JsonPropertyName("totalGross")] public decimal TotalGross { get; set; }
            [JsonPropertyName("type")] public JsonElement Type { get; set; }
            [JsonPropertyName("notes")] public string Notes { get; set; } = "";
            [JsonPropertyName("status")] public JsonElement Status { get; set; }
            [JsonPropertyName("categoryId")] public int? CategoryId { get; set; }
            [JsonPropertyName("isRecurring")] public bool IsRecurring { get; set; }
            [JsonPropertyName("frequencyUnit")] public int? FrequencyUnit { get; set; }
            [JsonPropertyName("intervalValue")] public int? IntervalValue { get; set; }
        }

        public class EditInvoiceDto
        {
            [JsonPropertyName("invoiceNumber")] public string InvoiceNumber { get; set; } = "";
            [JsonPropertyName("issueDate")] public DateTime IssueDate { get; set; }
            [JsonPropertyName("dueDate")] public DateTime DueDate { get; set; }
            [JsonPropertyName("totalNet")] public decimal TotalNet { get; set; }
            [JsonPropertyName("totalGross")] public decimal TotalGross { get; set; }
            [JsonPropertyName("type")] public JsonElement Type { get; set; }
            [JsonPropertyName("notes")] public string Notes { get; set; } = "";
            [JsonPropertyName("status")] public JsonElement Status { get; set; }
            [JsonPropertyName("categoryId")] public int? CategoryId { get; set; }
        }

        private int ParseType(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number) return element.GetInt32();
            if (element.ValueKind == JsonValueKind.String)
            {
                var s = element.GetString();
                if (s == "Sales") return 0;
                if (s == "Cost") return 1;
                if (int.TryParse(s, out int val)) return val;
            }
            return 0;
        }

        private int ParseStatus(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Number) return element.GetInt32();
            if (element.ValueKind == JsonValueKind.String)
            {
                var s = element.GetString();
                if (s == "Unpaid") return 0;
                if (s == "Paid") return 1;
                if (s == "PartiallyPaid") return 2;
                if (int.TryParse(s, out int val)) return val;
            }
            return 0;
        }

        public void Map(IEndpointRouteBuilder app)
        {
            // main view
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
            
            // load invoices list
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
                    categoryName = i.Category?.Name ?? "Brak",
                    issueDate = i.IssueDate.ToString("yyyy-MM-dd"),
                    dueDate = i.DueDate.ToString("yyyy-MM-dd"),
                    totalGross = i.TotalGross,
                    type = i.Type.ToString(),
                    status = i.Status.ToString(),
                    notes = i.Notes ?? ""
                });

                return Results.Json(result);
            });

            // listSome is used in Dashboard to show a few of the latest transactions
            app.MapGet("/api/invoices/listSome", async (HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null || employee.CompanyId == null) 
                    return Results.Content("<li class='transaction-item'><div class='transaction-main'><span>Brak autoryzacji.</span></div></li>", "text/html");

                var htmlBuilder = invoiceService.ListInvoicesForDashboard(employee.CompanyId.Value, 5);
                
                return Results.Content(htmlBuilder.ToString(), "text/html");
            });

            // load detailed invoice
            app.MapGet("/api/invoices/{id}", async (int id, AppDbContext db) =>
            {
                var invoice = await db.Invoices
                    .Include(i => i.Contractor)
                    .Include(i => i.Category)
                    .Include(i => i.RecurringOperation)
                    .FirstOrDefaultAsync(i => i.Id == id);

                if (invoice == null) return Results.NotFound();

                return Results.Json(new
                {
                    id = invoice.Id,
                    invoiceNumber = invoice.InvoiceNumber,
                    contractorName = invoice.Contractor?.Name ?? "Nieznany Kontrahent",
                    categoryName = invoice.Category?.Name ?? "Brak",
                    categoryId = invoice.CategoryId,
                    issueDate = invoice.IssueDate.ToString("yyyy-MM-dd"),
                    dueDate = invoice.DueDate.ToString("yyyy-MM-dd"),
                    totalNet = invoice.TotalNet,
                    totalGross = invoice.TotalGross,
                    type = invoice.Type.ToString(),
                    status = invoice.Status.ToString(),
                    notes = invoice.Notes ?? "",
                    isRecurring = invoice.RecurringOperation != null
                });
            });

            // add new invoice
            app.MapPost("/api/invoices", async (CreateInvoiceDto dto, HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });

                if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                    return Results.Json(new { success = false, message = "Numer faktury jest wymagany" });

                int typeVal = ParseType(dto.Type);
                int statusVal = ParseStatus(dto.Status);

                var result = invoiceService.AddInvoice(
                    employee.CompanyId.Value, dto.ContractorId, dto.InvoiceNumber, 
                    dto.IssueDate, dto.DueDate, (PaymentMethod)dto.PaymentMethod, 
                    dto.TotalNet, dto.TotalGross, (InvoiceType)typeVal, dto.Notes, (InvoiceStatus)statusVal,
                    dto.CategoryId,
                    dto.IsRecurring, dto.FrequencyUnit, dto.IntervalValue
                );

                if (result == "Pomyślnie dodano fakturę")
                {
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, message = result });
            });

            // Delete invoice
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

            // Edit invoice
            app.MapPut("/api/invoices/{id}", async (int id, EditInvoiceDto dto, HttpContext context, AppDbContext db, InvoiceService invoiceService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);
                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });
                if (string.IsNullOrWhiteSpace(dto.InvoiceNumber))
                    return Results.Json(new { success = false, message = "Numer faktury jest wymagany" });

                int typeVal = ParseType(dto.Type);
                int statusVal = ParseStatus(dto.Status);

                var result = invoiceService.EditInvoice(id, dto.InvoiceNumber, dto.IssueDate, dto.DueDate, dto.TotalNet, dto.TotalGross, (InvoiceType)typeVal, dto.Notes, (InvoiceStatus)statusVal, dto.CategoryId);
                if (result == "Pomyślnie zaktualizowano fakturę")
                {
                    return Results.Json(new { success = true });
                }
                return Results.Json(new { success = false, message = result });
            });
        }
    }
}
