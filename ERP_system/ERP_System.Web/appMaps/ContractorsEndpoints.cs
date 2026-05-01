using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace ERP_System.Web.appMaps
{
    public static class ContractorsEndpoints
    {
        public static void MapContractorEndpoints(this IEndpointRouteBuilder app)
        {
            // WYSWIETLANIE STRONY HTML
            app.MapGet("/contractors", async (HttpContext context, IWebHostEnvironment env, AppDbContext db) =>
            {
                // Sprawdzamy ciasteczko user_id (zgodnie z LoginEndpoint)
                if (!context.Request.Cookies.ContainsKey("logged_user"))
                    return Results.Redirect("/");

                var userId = int.Parse(context.Request.Cookies["user_id"]!);
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);
                var username = context.Request.Cookies["logged_user"];
                // context.Response.ContentType = "text/html; charset=utf-8";
                var filePath = Path.Combine(env.WebRootPath, "contractors.html");
                var html = File.ReadAllText(filePath, Encoding.UTF8);

                html = html.Replace("{username}", username);
                string adminBtnHtml = "";

                if (user.Role == SystemRole.SystemAdmin)
                {
                    adminBtnHtml = "<button class=\"sidebar-link\" onclick=\"window.location.href='/adminConsole'\"><i class=\"fas fa-fw fa-cogs\"></i> &nbsp; Ustawienia Admina</button>";
                }
                html = html.Replace("{admin_panel_button}", adminBtnHtml);
                return Results.Content(html, "text/html; charset=utf-8");
                // await context.Response.SendFileAsync("wwwroot/contractors.html");
            });                  
            
            // POBIERANIE LISTY
            app.MapGet("/api/contractors", async (HttpContext context, ContractorService service, AppDbContext db) =>
            {
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

                // Pobieramy użytkownika z bazy, żeby wyciągnąć jego CompanyId (tak jak w Dashboardzie)
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || user.CompanyId == null) return Results.Json(new List<DBContractor>());

                var contractors = service.GetCompanyContractors(user.CompanyId.Value);
                return Results.Json(contractors);
            });

            // DODAWANIE NOWEGO KONTRAHENTA
            app.MapPost("/api/contractors", async (HttpContext context, ContractorService service, AppDbContext db) =>
            {
                ContractorRequest? body;
                try {
                    body = await context.Request.ReadFromJsonAsync<ContractorRequest>();
                } catch {
                    return Results.BadRequest(new { success = false, message = "Nieprawidłowy format danych." });
                }

                if (body == null) return Results.BadRequest();

                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || user.CompanyId == null) return Results.BadRequest(new { message = "Nie należysz do żadnej firmy." });

                try 
                {
                    var contractor = service.AddContractor(
                        user.CompanyId.Value, 
                        body.Name, 
                        body.TaxId, 
                        body.Street, 
                        body.City, 
                        body.ZipCode, 
                        body.Email
                    );

                    return Results.Ok(new { success = true, id = contractor.Id });
                }
                catch (Exception ex)
                {
                    return Results.Ok(new { success = false, message = ex.Message });
                }
            });

            // USUWANIE
            app.MapDelete("/api/contractors/{id:int}", async (int id, HttpContext context, ContractorService service, AppDbContext db) =>
            {
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null || user.CompanyId == null) return Results.Unauthorized();

                var result = service.DeleteContractor(id, user.CompanyId.Value);
                return Results.Ok(new { success = true, message = result });
            });
        }
    }

    public record ContractorRequest(string Name, string TaxId, string Street, string City, string ZipCode, string Email);
}