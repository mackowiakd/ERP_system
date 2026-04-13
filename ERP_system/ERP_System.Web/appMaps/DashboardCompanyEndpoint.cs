using System.Net;
using System.Text;
using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Web.appMaps
{
    /// <summary>
    /// Handles the main /company route, displaying company information or the choice to create/join one.
    /// Uses HTML templates to avoid inline HTML in C# code.
    /// </summary>
    public class DashboardCompanyEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/company", async (HttpContext context, IWebHostEnvironment env, AppDbContext db) =>
            {
                // Authenticate user
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Redirect("/login");

                var user = await db.Employees.Include(u => u.Company).FirstOrDefaultAsync(u => u.Id == userId);
                var username = context.Request.Cookies["logged_user"];

                if (user == null)
                    return Results.Redirect("/login");

                // Determine Admin Button visibility (Only SystemAdmin)
                string adminBtnHtml = "";
                if (user.Role == SystemRole.SystemAdmin)
                {
                    adminBtnHtml = "<button class=\"sidebar-link\" onclick=\"window.location.href='/adminConsole'\"><i class=\"fas fa-fw fa-cogs\"></i> &nbsp; Panel Admina</button>";
                }

                // CASE 1: User has no company - Show choice page
                if (user.CompanyId == null)
                {
                    var filePath = Path.Combine(env.WebRootPath, "company.html");
                    if (!File.Exists(filePath)) return Results.NotFound("Błąd: Plik company.html nie istnieje.");

                    var html = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                    html = html.Replace("{username}", username)
                               .Replace("{admin_panel_button}", adminBtnHtml);

                    return Results.Content(html, "text/html; charset=utf-8");
                }

                // CASE 2: User has a company - Show details using template
                return await RenderCompanyViewWithTemplate(env, db, user, username ?? "Użytkownik", adminBtnHtml);
            });

            // Endpoint for removing members (AJAX/HTMX)
            app.MapPost("/remove-member", async (int userId, HttpContext context, AppDbContext db) =>
            {
                var login = context.Request.Cookies["logged_user"];
                if (string.IsNullOrEmpty(login)) return Results.Unauthorized();

                var adminUser = await db.Employees.FirstOrDefaultAsync(u => u.Login == login);
                if (adminUser == null || adminUser.Role != SystemRole.CompanyAdmin || adminUser.CompanyId == null)
                    return Results.Forbid();

                var targetUser = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);
                if (targetUser == null || targetUser.CompanyId != adminUser.CompanyId)
                    return Results.BadRequest("Użytkownik nie należy do Twojej firmy");

                targetUser.CompanyId = null;
                if (targetUser.Role != SystemRole.SystemAdmin) targetUser.Role = SystemRole.Guest;
                
                await db.SaveChangesAsync();
                return Results.Ok();
            });
        }

        /// <summary>
        /// Loads companyDetails.html template and injects company data.
        /// </summary>
        private static async Task<IResult> RenderCompanyViewWithTemplate(IWebHostEnvironment env, AppDbContext db, DBEmployee user, string username, string adminBtnHtml)
        {
            var company = await db.Companies
                .Include(c => c.Members)
                .FirstOrDefaultAsync(c => c.Id == user.CompanyId);

            if (company == null) return Results.Redirect("/company");

            var templatePath = Path.Combine(env.WebRootPath, "companyDetails.html");
            if (!File.Exists(templatePath)) return Results.NotFound("Błąd: Plik companyDetails.html nie istnieje.");

            var html = await File.ReadAllTextAsync(templatePath, Encoding.UTF8);
            bool iAmAdmin = user.Role == SystemRole.CompanyAdmin;

            // Generate members table rows
            var membersRows = new StringBuilder();
            var sortedMembers = company.Members.OrderByDescending(m => m.Role == SystemRole.CompanyAdmin).ThenBy(m => m.Login).ToList();

            foreach (var m in sortedMembers)
            {
                var isMe = string.Equals(m.Login, user.Login, StringComparison.OrdinalIgnoreCase);
                var isTargetAdmin = m.Role == SystemRole.CompanyAdmin;
                var roleDisplay = isTargetAdmin ? "Administrator" : "Pracownik";
                var loginDisplay = isMe ? $"{m.Login} (Ty)" : m.Login;

                string actionHtml = "";
                if (iAmAdmin && !isTargetAdmin)
                {
                    actionHtml = $"<button onclick=\"if(confirm('Usunąć {m.Login}?')) fetch('/remove-member?userId={m.Id}', {{method:'POST'}}).then(r=>location.reload())\" style='color:white; border:none; background:#dc3545; padding: 5px 10px; border-radius:4px; cursor:pointer;'>Usuń</button>";
                }

                membersRows.Append($@"
                    <tr style='border-bottom: 1px solid #eee;'>
                        <td style='padding: 12px;'>{m.Id}</td>
                        <td style='padding: 12px;'>{loginDisplay}</td>
                        <td style='padding: 12px;'>{m.Email}</td>
                        <td style='padding: 12px;'>{roleDisplay}</td>
                        <td style='padding: 12px; text-align: center;'>{actionHtml}</td>
                    </tr>");
            }

            // Prepare Leave Button texts
            string leaveButtonText = iAmAdmin ? "Usuń firmę (jesteś adminem)" : "Opuść firmę";
            string leaveConfirmText = iAmAdmin 
                ? "Jako administrator, opuszczając firmę, spowodujesz jej trwałe usunięcie wraz z całą historią. Czy na pewno chcesz kontynuować?" 
                : "Czy na pewno chcesz opuścić struktury firmy?";

            // Inject data into template
            html = html.Replace("{username}", username)
                       .Replace("{admin_panel_button}", adminBtnHtml)
                       .Replace("{companyShortName}", WebUtility.HtmlEncode(company.ShortName))
                       .Replace("{companyFullName}", WebUtility.HtmlEncode(company.FullName))
                       .Replace("{companyDescription}", WebUtility.HtmlEncode(company.Description ?? "Brak opisu firmy."))
                       .Replace("{companyNip}", WebUtility.HtmlEncode(company.NIP))
                       .Replace("{companyAddress}", WebUtility.HtmlEncode(company.Address))
                       .Replace("{companyJoinCode}", WebUtility.HtmlEncode(company.JoinCode))
                       .Replace("{memberCount}", company.Members.Count.ToString())
                       .Replace("{membersRows}", membersRows.ToString())
                       .Replace("{leaveButtonText}", leaveButtonText)
                       .Replace("{leaveConfirmText}", leaveConfirmText);

            return Results.Content(html, "text/html; charset=utf-8");
        }
    }
}
