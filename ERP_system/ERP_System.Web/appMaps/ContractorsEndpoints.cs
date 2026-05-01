using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Web.appMaps
{
    public static class ContractorsEndpoints
    {
        public static void MapContractorEndpoints(this IEndpointRouteBuilder app)
        {
            // WYSWIETLANIE STRONY HTML
            app.MapGet("/contractors", async context =>
            {
                // Sprawdzamy ciasteczko user_id (zgodnie z LoginEndpoint)
                if (!context.Request.Cookies.ContainsKey("user_id"))
                {
                    context.Response.Redirect("/");
                    return;
                }

                context.Response.ContentType = "text/html; charset=utf-8";
                await context.Response.SendFileAsync("wwwroot/contractors.html");
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