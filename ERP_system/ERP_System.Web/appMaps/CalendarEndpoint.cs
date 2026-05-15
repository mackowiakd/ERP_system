using System.Text;
using Microsoft.EntityFrameworkCore;
using ERP_System.Core;
using ERP_System.Core.DBTables;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace ERP_System.Web.appMaps
{
    public class CalendarEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/calendar", async (HttpContext context, IWebHostEnvironment env, AppDbContext db) =>
            {
                if (!context.Request.Cookies.ContainsKey("logged_user"))
                    return Results.Redirect("/");

                var userId = int.Parse(context.Request.Cookies["user_id"]!);
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                {
                    return Results.Content("brak uzytkownika o takiej nazwie");
                }

                var username = context.Request.Cookies["logged_user"];
                var filePath = Path.Combine(env.WebRootPath, "calendar.html");
                var html = File.ReadAllText(filePath, Encoding.UTF8);

                html = html.Replace("{username}", username);
                string adminBtnHtml = "";

                if (user.Role == SystemRole.SystemAdmin)
                {
                    adminBtnHtml = "<button class=\"sidebar-link\" onclick=\"window.location.href='/adminConsole'\"><i class=\"fas fa-fw fa-cogs\"></i> &nbsp; Ustawienia Admina</button>";
                }
                html = html.Replace("{admin_panel_button}", adminBtnHtml);
                return Results.Content(html, "text/html; charset=utf-8");
            });

            app.MapGet("/api/calendar-events", async (HttpContext context, AppDbContext db) =>
            {
                if (!context.Request.Cookies.ContainsKey("logged_user"))
                    return Results.Unauthorized();

                var userId = int.Parse(context.Request.Cookies["user_id"]!);
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || !user.CompanyId.HasValue)
                    return Results.Unauthorized();

                int companyId = user.CompanyId.Value;

                // load date from ui
                DateTime startDate = DateTime.MinValue;
                DateTime endDate = DateTime.MaxValue;

                if (DateTime.TryParse(context.Request.Query["start"], out var s)) startDate = s;
                if (DateTime.TryParse(context.Request.Query["end"], out var e)) endDate = e;

                var events = new List<dynamic>();

                // regular invoice
                var invoices = await db.Invoices
                    .Include(i => i.Contractor)
                    .Include(i => i.RecurringOperation)
                    .Where(i => i.CompanyId == companyId && i.IssueDate >= startDate && i.IssueDate <= endDate)
                    .ToListAsync();

                foreach (var i in invoices)
                {
                    events.Add(new
                    {
                        id = i.Id.ToString(),
                        title = i.InvoiceNumber,
                        startTime = i.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endTime = i.DueDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        amount = i.TotalGross,
                        type = (int)i.Type,
                        status = (int)i.Status,
                        description = $"Faktura: {i.InvoiceNumber} | Kontrahent: {i.Contractor?.Name ?? "Nieznany"}",
                        description2 = $"Typ: {(i.Type == InvoiceType.Sales ? "Sprzedażowa" : "Kosztowa")} | Status: {(i.Status == InvoiceStatus.Paid ? "Opłacona" : "Nieopłacona")}",
                        description3 = i.Notes ?? "",
                        color = i.Type == InvoiceType.Cost ? "#e74a3b" : "#1cc88a",
                        isRecurring = false,
                        hasRecurringRule = i.RecurringOperation != null
                    });
                }

                // regular transactions (FinancialOperations)
                var transactions = await db.FinancialOperations
                    .Include(t => t.RecurringOperation)
                    .Where(t => t.CompanyId == companyId && t.Date >= startDate && t.Date <= endDate)
                    .ToListAsync();

                foreach (var t in transactions)
                {
                    events.Add(new
                    {
                        id = "trans_" + t.Id,
                        title = t.Title,
                        startTime = t.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endTime = t.Date.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                        amount = t.Value,
                        type = (int)t.TransactionType,
                        status = 1, // consider as paid
                        description = $"Transakcja: {t.Title}",
                        description2 = $"Opis: {t.Description ?? "Brak"}",
                        description3 = "",
                        color = t.Value < 0 ? "#e74a3b" : "#1cc88a",
                        isRecurring = false,
                        hasRecurringRule = t.RecurringOperation != null
                    });
                }

                // reccuring items (Invoices and Transactions)
                var recurringOps = await db.RecurringOperations
                    .Include(r => r.BaseInvoice)
                    .ThenInclude(i => i.Contractor)
                    .Include(r => r.Invoice)
                    .Where(r => (r.BaseInvoice != null && r.BaseInvoice.CompanyId == companyId || r.Invoice != null && r.Invoice.CompanyId == companyId) && r.IsActive)
                    .ToListAsync();

                foreach (var ro in recurringOps)
                {
                    var isInvoice = ro.BaseInvoice != null;
                    var titlePattern = isInvoice ? ro.BaseInvoice!.InvoiceNumber : ro.Invoice!.Title;
                    var amount = isInvoice ? ro.BaseInvoice!.TotalGross : ro.Invoice!.Value;
                    var type = isInvoice ? (int)ro.BaseInvoice!.Type : (int)ro.Invoice!.TransactionType;
                    var description = isInvoice ? $"Faktura CYKLICZNA: {ro.BaseInvoice.InvoiceNumber} | Kontrahent: {ro.BaseInvoice.Contractor?.Name ?? "Nieznany"}" : $"Transakcja CYKLICZNA: {ro.Invoice!.Title}";

                    var current = ro.NextRunDate;
                    var intervalType = (ERP_System.Core.Enums.TransactionIntervalType)ro.IntervalType;

                    while (current < startDate)
                    {
                        current = intervalType switch
                        {
                            ERP_System.Core.Enums.TransactionIntervalType.Days => current.AddDays(ro.IntervalValue),
                            ERP_System.Core.Enums.TransactionIntervalType.Weeks => current.AddDays(ro.IntervalValue * 7),
                            ERP_System.Core.Enums.TransactionIntervalType.Months => current.AddMonths(ro.IntervalValue),
                            ERP_System.Core.Enums.TransactionIntervalType.Years => current.AddYears(ro.IntervalValue),
                            _ => current.AddMonths(1)
                        };
                    }

                    int safety = 0;
                    while (current <= endDate && safety < 100) // Project all occurrences within range for the grid
                    {
                        events.Add(new
                        {
                            id = "rec_" + ro.Id + "_" + current.Ticks,
                            title = "[CYKL] " + titlePattern,
                            startTime = current.ToString("yyyy-MM-ddTHH:mm:ss"),
                            endTime = current.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                            amount = amount,
                            type = type,
                            status = 0,
                            description = description,
                            description2 = "Status: Planowana",
                            description3 = "",
                            color = "#f6c23e",
                            isRecurring = true
                        });

                        current = intervalType switch
                        {
                            ERP_System.Core.Enums.TransactionIntervalType.Days => current.AddDays(ro.IntervalValue),
                            ERP_System.Core.Enums.TransactionIntervalType.Weeks => current.AddDays(ro.IntervalValue * 7),
                            ERP_System.Core.Enums.TransactionIntervalType.Months => current.AddMonths(ro.IntervalValue),
                            ERP_System.Core.Enums.TransactionIntervalType.Years => current.AddYears(ro.IntervalValue),
                            _ => current.AddMonths(1)
                        };
                        safety++;
                    }
                }

                return Results.Json(events);
            });
        }
    }
}