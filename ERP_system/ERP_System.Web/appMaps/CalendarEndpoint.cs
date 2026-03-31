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

                if (user.Role == SystemRole.SystemAdmin || user.Role == SystemRole.CompanyAdmin)
                {
                    adminBtnHtml = "<button class=\"sidebar-link\" onclick=\"window.location.href='/adminConsole'\"><i class=\"fas fa-fw fa-cogs\"></i> &nbsp; Ustawienia Admina</button>";
                }
                html = html.Replace("{admin_panel_button}", adminBtnHtml);
                return Results.Content(html, "text/html; charset=utf-8");
            });

            _ = app.MapGet("/api/calendar-events", async (HttpContext context, AppDbContext db) =>
            {
                if (!context.Request.Cookies.ContainsKey("logged_user"))
                    return Results.Unauthorized();

                var userId = int.Parse(context.Request.Cookies["user_id"]!);
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return Results.Unauthorized();

                List<int> userIds = new List<int>();
                if (user.CompanyId.HasValue)
                {
                    userIds = await db.Employees
                        .Where(u => u.CompanyId == user.CompanyId.Value)
                        .Select(u => u.Id)
                        .ToListAsync();
                }
                else
                {
                    userIds.Add(user.Id);
                }
                List<int> companyIds = new List<int>();
                {
                    if (user.CompanyId.HasValue)
                    {
                        companyIds.Add(user.CompanyId.Value);
                    }
                }
                List<dynamic> invoices = new List<dynamic>();
                /*List<dynamic> transactions = new List<dynamic>();

                var regularTransactions = await db.FinancialOperations
                    .Include(t => t.Employee)
                    .Where(t => userIds.Contains(t.EmployeeId)) // Poprawiono z CompanyId na EmployeeId
                    .OrderBy(t => t.Date)
                    .Select(t => new
                    {
                        id = t.Id.ToString(),
                        title = t.Title,
                        startTime = t.Date.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endTime = t.Date.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                        amount = t.Value,
                        description = t.Description ?? "",
                        categoryId = t.CategoryId,
                        color = t.Value < 0 ? "#e74a3b" : "#1cc88a",
                        reminder = false,
                        isRecurring = false
                    })
                    .ToListAsync();*/

                /*transactions.AddRange(regularTransactions);

                var repetableTransactions = await db.RecurringOperations
                    .Include(rt => rt.Transaction) // ZMIANA: Poprawne użycie Include
                        .ThenInclude(t => t.Employee)
                    .Where(rt => rt.Transaction != null && userIds.Contains(rt.Transaction.EmployeeId) && rt.IsActive)
                    .ToListAsync();

                foreach (var rt in repetableTransactions)
                {
                    var baseTitle = !string.IsNullOrWhiteSpace(rt.Transaction!.Title)
                        ? rt.Transaction.Title
                        : "Brak tytułu";

                    var nextDate = rt.NextRunDate;
                    for (int i = 0; i < 12; i++) // Generate next 12 occurrences
                    {
                        transactions.Add(new
                        {
                            id = rt.Transaction.Id.ToString(), // ZMIANA: Idk zamiast całego obiektu
                            title = $"Cykliczna: {baseTitle}",
                            startTime = nextDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            endTime = nextDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                            amount = rt.Transaction.Value, // ZMIANA: Value (kwota) zamiast IntervalValue (czasu)
                            description = rt.Transaction.Description ?? "",
                            categoryId = rt.Transaction.CategoryId,
                            color = "#f6c23e",
                            reminder = false,
                            isRecurring = true
                        });

                        // ZMIANA: IntervalType zamiast FrequencyUnit, IntervalValue zamiast TransactionInterval
                        nextDate = rt.IntervalType switch
                        {
                            0 => nextDate.AddDays(rt.IntervalValue),
                            1 => nextDate.AddDays(rt.IntervalValue * 7),
                            2 => nextDate.AddMonths(rt.IntervalValue),
                            3 => nextDate.AddYears(rt.IntervalValue),
                            _ => nextDate.AddMonths(1),
                        };
                    }
                }*/
                var selectedInvoices = await db.Invoices
                    .Where(i => companyIds.Contains(i.CompanyId))
                    .OrderBy(i => i.DueDate)
                    .Select(i => new
                    {
                        id = i.Id.ToString(),
                        title = $"Faktura: {i.InvoiceNumber}",
                        startTime = i.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                        endTime = i.DueDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                        amount = i.TotalGross,
                        direction = i.Type,
                        description = "Kontrahent: " + (i.Contractor != null ? i.Contractor.Name : "Nieznany")
                        + " || Typ: " + (i.Type == InvoiceType.Sales ? "Sprzedażowa" : "Kosztowa")
                        + ", Status: " + (i.Status == InvoiceStatus.Paid ? "Opłacona" : "Nieopłacona"),
                        categoryId = (int?)null,
                        color = i.Type == InvoiceType.Cost ? "#e74a3b" : "#1cc88a",
                        reminder = false,
                        isRecurring = false
                    })
                    .ToListAsync();
                    invoices.AddRange(selectedInvoices);
                return Results.Json(invoices);
            });
        }
    }
}