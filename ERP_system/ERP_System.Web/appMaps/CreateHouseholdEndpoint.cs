using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERP_System.Web.appMaps
{
    public class CreateHouseholdEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapGet("/createHousehold.html", async (HttpContext context, IWebHostEnvironment env, AppDbContext db) =>
            {
                if (!context.Request.Cookies.ContainsKey("logged_user"))
                    return Results.Redirect("/");

                var username = context.Request.Cookies["logged_user"].ToString();
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == username);
                if (user == null) return Results.Redirect("/");

                var filePath = Path.Combine(env.WebRootPath, "createHousehold.html");
                if (!File.Exists(filePath)) return Results.NotFound();

                var html = await File.ReadAllTextAsync(filePath, Encoding.UTF8);
                
                string adminBtnHtml = "";
                if (user.Role == SystemRole.SystemAdmin)
                {
                    adminBtnHtml = "<button class=\"sidebar-link\" onclick=\"window.location.href='/adminConsole'\"><i class=\"fas fa-fw fa-cogs\"></i> &nbsp; Ustawienia Admina</button>";
                }

                html = html.Replace("{username}", username)
                           .Replace("{admin_panel_button}", adminBtnHtml);

                return Results.Content(html, "text/html; charset=utf-8");
            });

            app.MapPost("/create-household", async (HttpContext context, AppDbContext db) =>
            {
                var form = context.Request.Form;
                var name = form["name"];
                var description = form["description"];
                var userLogin = context.Request.Cookies["logged_user"];

                if (string.IsNullOrWhiteSpace(name))
                {
                    return Results.Content("<div class='error'>Błąd: nazwa firmy jest wymagana.</div>", "text/html");
                }

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == userLogin);
                if (user == null)
                {
                    return Results.Content("<div class='error'>Błąd: użytkownik niezalogowany.</div>", "text/html");
                }

                if (user.CompanyId != null)
                {
                    return Results.Content("<div class='error'>Błąd: użytkownik należy już do firmy.</div>", "text/html");
                }

                // create household
                var company = new DBCompany
                {
                    Name = name,
                    NIP = "0000000000", // default NIP, can be changed later by admin
                    CompanyAdmin = user,
                    Description = description,
                    CompanyAdminId = user.Id,
                    JoinCode = Guid.NewGuid().ToString("N").Substring(0, 6).ToUpper() // ex. "A1B2C3"
                };
                db.Companies.Add(company);
                await db.SaveChangesAsync();

                // set user comapnyhold admin
                user.CompanyId = company.Id;
                
                if (user.Role != SystemRole.CompanyAdmin)
                {
                    user.Role = SystemRole.CompanyAdmin;
                }

                await db.SaveChangesAsync();

                return Results.Content("<div class='success'>Firma utworzona!</div>", "text/html");
            });

        }
    }
}
