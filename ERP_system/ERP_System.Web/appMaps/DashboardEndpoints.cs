using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Microsoft.EntityFrameworkCore;
using static ERP_System.Core.ChartService;
namespace ERP_System.Web.appMaps
{
    public class DashboardEndpoints : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/dashboard", async (HttpContext context, IWebHostEnvironment env, AppDbContext db, ChartService chartService) =>
            {
                // check login status
                var userId = int.Parse(context.Request.Cookies["user_id"]);
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);



                var username = context.Request.Cookies["logged_user"];

                if (user == null)
                {
                    return Results.Redirect("/");
                }

                
                var now = DateTime.Now;
                var startMonth = new DateTime(now.Year, now.Month, 1);

                // Liczymy saldo na podstawie faktur z bieżącego miesiąca
                var invoices = await db.Invoices
                    .Where(i => i.CompanyId == (user.CompanyId ?? 0) && i.IssueDate >= startMonth)
                    .ToListAsync();

                decimal currentMonthBalance = 0;
                foreach (var inv in invoices)
                {
                    if (inv.Type == InvoiceType.Cost)
                        currentMonthBalance -= inv.TotalGross;
                    else
                        currentMonthBalance += inv.TotalGross;
                }

                var filePath = Path.Combine(env.WebRootPath, "dashboard.html");
                // load html
                var html = File.ReadAllText(filePath);
                string adminBtnHtml = "";
                
                if (user.Role == SystemRole.SystemAdmin)
                {
                    adminBtnHtml = "<button class=\"sidebar-link\" onclick=\"window.location.href='/adminConsole'\"><i class=\"fas fa-fw fa-cogs\"></i> &nbsp; Ustawienia Admina</button>";
                }
                // replace placeholders with correct values
                List<int> IDs = new List<int>();
                int currentID = int.Parse(context.Request.Cookies["user_id"]);
                IDs.Add(currentID);
                
                var absoluteStartDate = new DateTime(2000, 1, 1);
                (List<CategoryStat> Expenses, List<CategoryStat> Incomes) = chartService.GetStatistics(new List<int> { user.CompanyId ?? 0 }, startMonth, now);
                
                html =  html.Replace("{username}", username)
                            .Replace("{balance}", currentMonthBalance.ToString("N2"))
                            .Replace("{admin_panel_button}", adminBtnHtml);
                return Results.Content(html, "text/html");
            });

            app.MapGet("/dashboard/charts", (HttpContext context, ChartService chartService) =>
            {
                var userIdentifier = context.Request.Cookies["user_id"];
                if (string.IsNullOrEmpty(userIdentifier))
                {
                    return Results.Content("");
                }
                int userId =int.Parse(userIdentifier);
                string chartsHtml = chartService.GenerateDashboardChartsHtml(userId);
                

                return Results.Content(chartsHtml, "text/html");
            });
        }
    }
}