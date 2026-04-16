using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERP_System.Web.appMaps
{
    /// <summary>
    /// Handles endpoints related to creating a new company.
    /// </summary>
    public class CreateCompanyEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            // GET /create-company-view - Returns the company creation HTML page.
            app.MapGet("/create-company-view", async (HttpContext context, IWebHostEnvironment env, AppDbContext db) =>
            {
                if (!context.Request.Cookies.ContainsKey("logged_user"))
                    return Results.Redirect("/login");

                var username = context.Request.Cookies["logged_user"].ToString();
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == username);
                if (user == null) return Results.Redirect("/login");

                var filePath = Path.Combine(env.WebRootPath, "createCompany.html");
                if (!File.Exists(filePath)) 
                    return Results.NotFound("Błąd: Plik createCompany.html nie został odnaleziony.");

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

            // POST /create-company - Process form data to create a new company.
            app.MapPost("/create-company", async (HttpContext context, AppDbContext db) =>
            {
                var form = context.Request.Form;
                var shortName = form["name"].ToString();
                var fullName = form["fullName"].ToString();
                var nip = form["nip"].ToString();
                var address = form["address"].ToString();
                var description = form["description"].ToString();
                var userLogin = context.Request.Cookies["logged_user"];

                // Validation
                if (string.IsNullOrWhiteSpace(shortName) || string.IsNullOrWhiteSpace(fullName) || 
                    string.IsNullOrWhiteSpace(nip) || string.IsNullOrWhiteSpace(address))
                {
                    return Results.Content("<div class='error' style='color: red; margin-top: 10px;'>Błąd: Wszystkie pola (poza opisem) są wymagane.</div>", "text/html");
                }

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == userLogin);
                if (user == null)
                {
                    return Results.Content("<div class='error' style='color: red; margin-top: 10px;'>Błąd: użytkownik niezalogowany.</div>", "text/html");
                }

                if (user.CompanyId != null)
                {
                    return Results.Content("<div class='error' style='color: red; margin-top: 10px;'>Błąd: użytkownik należy już do firmy.</div>", "text/html");
                }

                // Create new company entity with expanded details
                var company = new DBCompany
                {
                    ShortName = shortName,
                    FullName = fullName,
                    NIP = nip,
                    Address = address,
                    Description = description,
                    CompanyAdmin = user,
                    CompanyAdminId = user.Id,
                    JoinCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper() // e.g. "XJ3K9P"
                };
                db.Companies.Add(company);
                await db.SaveChangesAsync();

                // Assign the user as the company administrator
                user.CompanyId = company.Id;
                if (user.Role != SystemRole.SystemAdmin)
                {
                    user.Role = SystemRole.CompanyAdmin;
                }

                await db.SaveChangesAsync();

                return Results.Content("<div class='success' style='color: green; margin-top: 10px;'>Firma została utworzona pomyślnie! <a href='/dashboard' style='font-weight: bold;'>Przejdź do pulpitu</a></div>", "text/html");
            });

        }
    }
}
