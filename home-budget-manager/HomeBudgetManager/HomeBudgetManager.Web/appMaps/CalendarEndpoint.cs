using System.Text;
using Microsoft.EntityFrameworkCore;
using HomeBudgetManager.Core;
using HomeBudgetManager.Core.DBTables;

namespace HomeBudgetManager.Web.appMaps
{
    public class CalendarEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/calendar", async (HttpContext context, IWebHostEnvironment env, AppDbContext db) =>
            {
                if (!context.Request.Cookies.ContainsKey("logged_user"))
                    return Results.Redirect("/");

                var userId = int.Parse(context.Request.Cookies["user_id"]);
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
                var userId = int.Parse(context.Request.Cookies["user_id"]);
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);

                var username = context.Request.Cookies["logged_user"];
                if (user == null)
                    return Results.Unauthorized();

                List<int> employeeIds = new List<int>();
                if (user.CompanyId.HasValue)
                {
                    employeeIds = await db.Employees
                        .Where(u => u.CompanyId == user.CompanyId.Value)
                        .Select(u => u.Id)
                        .ToListAsync();
                }
                else
                {
                    employeeIds.Add(user.Id);
                }

                List<dynamic> transactions = new List<dynamic>();

                var regularTransactions = await db.FinancialOperations
                    .Include(t => t.Employee) // Include Employee to get Login
                    .Where(t => employeeIds.Contains(t.EmployeeId))
                    .OrderBy(t => t.Date) // Sort by date/time
                    .Select(t => new
                    {
                        id = t.Id.ToString(),
                        title = t.Title, // Use actual Title
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

                var recurringOperations = await db.RecurringOperations
                    .Include(rt => rt.Transaction)
                    .ThenInclude(t => t.Employee)
                    .Where(rt => rt.Transaction != null && employeeIds.Contains(rt.Transaction.EmployeeId) && rt.IsActive)
                    .ToListAsync();

                foreach (var rt in recurringOperations)
                {
                    if (rt.Transaction == null) continue;

                    var baseTitle = !string.IsNullOrWhiteSpace(rt.Transaction.Title) ? rt.Transaction.Title : "Brak tytułu";

                    var nextDate = rt.NextRunDate;
                    for (int i = 0; i < 12; i++) // Generate next 12 occurrences
                    {
                        transactions.Add(new
                        {
                            id = rt.TransactionPatternId.ToString(),
                            title = $"Cykliczna: {baseTitle}",
                            startTime = nextDate.ToString("yyyy-MM-ddTHH:mm:ss"),
                            endTime = nextDate.AddHours(1).ToString("yyyy-MM-ddTHH:mm:ss"),
                            amount = rt.Transaction.Value,
                            description = rt.Transaction.Description ?? "",
                            categoryId = rt.Transaction.CategoryId,
                            color = "#f6c23e", // Yellow for recurring
                            reminder = false,
                            isRecurring = true
                        });

                        nextDate = rt.IntervalType switch
                        {
                            0 => nextDate.AddDays(rt.IntervalValue),
                            1 => nextDate.AddDays(rt.IntervalValue * 7), // Weeks
                            2 => nextDate.AddMonths(rt.IntervalValue),   // Months
                            3 => nextDate.AddYears(rt.IntervalValue),    // Years
                            _ => nextDate.AddMonths(1),
                        };
                    }
                }


                return Results.Json(transactions);
            });
        }
    }
}
