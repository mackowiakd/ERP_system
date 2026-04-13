using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERP_System.Web.appMaps
{
    /// <summary>
    /// Handles endpoints related to joining an existing company.
    /// </summary>
    public class JoinCompanyEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            // GET /join-company-view - Returns the company joining HTML page.
            app.MapGet("/join-company-view", async (HttpContext context, IWebHostEnvironment env, AppDbContext db) =>
            {
                if (!context.Request.Cookies.ContainsKey("logged_user"))
                    return Results.Redirect("/login");
                
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Redirect("/login");

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return Results.Redirect("/login");

                var username = context.Request.Cookies["logged_user"];

                // Serving the correct joinCompany.html file
                var filePath = Path.Combine(env.WebRootPath, "joinCompany.html");
                if (!File.Exists(filePath)) 
                    return Results.NotFound("Błąd: Plik joinCompany.html nie został odnaleziony.");

                var html = await File.ReadAllTextAsync(filePath, Encoding.UTF8);

                string adminBtnHtml = "";
                // Restricted access: Only SystemAdmin can see the admin console button
                if (user.Role == SystemRole.SystemAdmin)
                {
                    adminBtnHtml = "<button class=\"sidebar-link\" onclick=\"window.location.href='/adminConsole'\"><i class=\"fas fa-fw fa-cogs\"></i> &nbsp; Panel Admina</button>";
                }

                html = html.Replace("{username}", username)
                           .Replace("{admin_panel_button}", adminBtnHtml);

                return Results.Content(html, "text/html; charset=utf-8");
            });

            // POST /join-company - Processes a join code to add the user to a company.
            app.MapPost("/join-company", async (HttpContext context, AppDbContext db) =>
            {
                var code = context.Request.Form["code"].ToString().ToUpper();
                var login = context.Request.Cookies["logged_user"];

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == login);
                if (user == null || user.CompanyId != null)
                {
                    return Results.Content("<div class='error' style='color: red; margin-top: 10px;'>Błąd: Nie możesz dołączyć do nowej firmy w tym momencie.</div>", "text/html");
                }

                // Find company by its unique join code
                var company = await db.Companies.FirstOrDefaultAsync(h => h.JoinCode == code);
                if (company == null)
                {
                    return Results.Content("<div class='error' style='color: red; margin-top: 10px;'>Nie znaleziono firmy o podanym kodzie.</div>", "text/html");
                }

                user.CompanyId = company.Id;
                
                // Assign Employee role if the user doesn't have a special system role
                if (user.Role != SystemRole.SystemAdmin && user.Role != SystemRole.CompanyAdmin)
                {
                    user.Role = SystemRole.Employee;
                }

                await db.SaveChangesAsync();

                return Results.Content("<div class='success' style='color: green; margin-top: 10px;'>Pomyślnie dołączono do firmy! <a href='/dashboard' style='font-weight: bold;'>Przejdź do pulpitu</a></div>", "text/html");
            });

        }
    }
}
