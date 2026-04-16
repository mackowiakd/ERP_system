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

                // Parametry daty z frontendu (jeśli podane)
                DateTime startDate = DateTime.MinValue;
                DateTime endDate = DateTime.MaxValue;

                if (DateTime.TryParse(context.Request.Query["start"], out var s)) startDate = s;
                if (DateTime.TryParse(context.Request.Query["end"], out var e)) endDate = e;

                var events = new List<dynamic>();

                // 1. Zwykłe faktury
                var invoices = await db.Invoices
                    .Include(i => i.Contractor)
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
                        isRecurring = false
                    });
                }

                // 2. Projekcja faktur cyklicznych
                var recurringOps = await db.RecurringOperations
                    .Include(r => r.BaseInvoice)
                    .ThenInclude(i => i.Contractor)
                    .Where(r => r.BaseInvoice != null && r.BaseInvoice.CompanyId == companyId && r.IsActive)
                    .ToListAsync();

                foreach (var ro in recurringOps)
                {
                    var inv = ro.BaseInvoice!;
                    var current = inv.IssueDate;
                    var intervalType = (ERP_System.Core.Enums.TransactionIntervalType)ro.IntervalType;

                    // Przesuń się do pierwszej daty w zakresie lub startowej
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

                    // Generuj wystąpienia w zakresie (max 100 dla bezpieczeństwa na jedną regułę)
                    int safety = 0;
                    while (current <= endDate && safety < 100)
                    {
                        // Pomijamy datę bazową jeśli już została dodana jako zwykła faktura
                        if (current != inv.IssueDate || !invoices.Any(x => x.Id == inv.Id))
                        {
                            events.Add(new
                            {
                                id = "rec_" + inv.Id + "_" + current.Ticks,
                                title = "[CYKL] " + inv.InvoiceNumber,
                                startTime = current.ToString("yyyy-MM-ddTHH:mm:ss"),
                                endTime = current.AddDays(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                                amount = inv.TotalGross,
                                type = (int)inv.Type,
                                status = (int)inv.Status,
                                description = $"Faktura CYKLICZNA: {inv.InvoiceNumber} | Kontrahent: {inv.Contractor?.Name ?? "Nieznany"}",
                                description2 = $"Typ: {(inv.Type == InvoiceType.Sales ? "Sprzedażowa" : "Kosztowa")} | Status: Planowana",
                                description3 = inv.Notes ?? "",
                                color = "#f6c23e", // Kolor żółty dla cyklicznych
                                isRecurring = true
                            });
                        }

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