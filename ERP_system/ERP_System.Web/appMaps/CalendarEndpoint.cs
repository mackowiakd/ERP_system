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

                if (user == null)
                    return Results.Unauthorized();

                List<dynamic> transactions = new List<dynamic>();

                var regularTransactions = await db.FinancialOperations
                    .Include(t => t.Employee)
                    .Where(t => user.CompanyId.HasValue ? t.CompanyId == user.CompanyId.Value : t.EmployeeId == user.Id)
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
                    .ToListAsync();

                transactions.AddRange(regularTransactions);

                // load invoices to calendar
                if (user.CompanyId.HasValue)
                {
                    var invoices = await db.Invoices
                        .Where(i => i.CompanyId == user.CompanyId.Value)
                        .Select(i => new
                        {
                            id = "inv_" + i.Id,
                            title = "Faktura: " + i.InvoiceNumber,
                            startTime = i.IssueDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            endTime = i.IssueDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                            amount = i.Type == InvoiceType.Cost ? -i.TotalGross : i.TotalGross,
                            description = "Termin płatności: " + i.DueDate.ToString("yyyy-MM-dd"),
                            categoryId = 0,
                            color = i.Type == InvoiceType.Cost ? "#e74a3b" : "#4e73df", // red for costs green for income
                            reminder = false,
                            isRecurring = false
                        })
                        .ToListAsync();
                    transactions.AddRange(invoices);
                }
                // ----------------------------------------------

                var repetableTransactions = await db.RecurringOperations
                    .Include(rt => rt.Transaction)
                        .ThenInclude(t => t.Employee)
                    .Where(rt => rt.Transaction != null && (user.CompanyId.HasValue ? rt.Transaction.CompanyId == user.CompanyId.Value : rt.Transaction.EmployeeId == user.Id) && rt.IsActive)
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
                            id = rt.Transaction.Id.ToString(), 
                            title = $"Cykliczna: {baseTitle}",
                            startTime = nextDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            endTime = nextDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                            amount = rt.Transaction.Value, 
                            description = rt.Transaction.Description ?? "",
                            categoryId = rt.Transaction.CategoryId,
                            color = "#f6c23e",
                            reminder = false,
                            isRecurring = true
                        });

                        nextDate = rt.IntervalType switch
                        {
                            0 => nextDate.AddDays(rt.IntervalValue),
                            1 => nextDate.AddDays(rt.IntervalValue * 7),
                            2 => nextDate.AddMonths(rt.IntervalValue),
                            3 => nextDate.AddYears(rt.IntervalValue),
                            _ => nextDate.AddMonths(1),
                        };
                    }
                }

                return Results.Json(transactions);
            });
        }
    }
}