using ERP_System.Core;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Web.appMaps
{
    public class ContractorsEndpoints : IEndpoint
    {
        public record CreateContractorDto(string Name, string TaxId, string Address);

        public void Map(IEndpointRouteBuilder app)
        {
            // 1. ZWRACANIE WIDOKU HTML (Strona)
            app.MapGet("/contractors", async (HttpContext context, AppDbContext db) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (user == null)
                {
                    context.Response.Redirect("/login");
                    return Results.Empty;
                }

                var html = await System.IO.File.ReadAllTextAsync("wwwroot/contractors.html");
                html = html.Replace("{username}", user.Login);
                
                // POPRAWKA: Dodano .ToString() aby poprawnie porównać rolę
                if (user.Role.ToString() == "Admin") {
                    html = html.Replace("{admin_panel_button}", "<button class=\"sidebar-link\" onclick=\"window.location.href='/admin'\"><i class=\"fas fa-cog\"></i> &nbsp; Panel Firmy</button>");
                } else {
                    html = html.Replace("{admin_panel_button}", "");
                }

                return Results.Content(html, "text/html");
            });

            // 2. POBIERANIE LISTY (API)
            app.MapGet("/api/contractors", async (HttpContext context, AppDbContext db, ContractorService contractorService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                // POPRAWKA: Sprawdzamy, czy CompanyId nie jest nullem
                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });

                // POPRAWKA: Używamy .Value
                var contractors = contractorService.GetCompanyContractors(employee.CompanyId.Value);
                
                var result = contractors.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    address = c.Address,
                    taxId = c.TaxId
                });

                return Results.Json(result);
            });

            // 3. DODAWANIE KONTRAHENTA (API)
            app.MapPost("/api/contractors", async (CreateContractorDto dto, HttpContext context, AppDbContext db, ContractorService contractorService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                // POPRAWKA: Sprawdzamy, czy CompanyId nie jest nullem
                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });

                if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.TaxId))
                    return Results.Json(new { success = false, message = "Nazwa i NIP są wymagane" });

                // POPRAWKA: Używamy .Value
                var result = contractorService.AddContractor(employee.CompanyId.Value, dto.Name, dto.TaxId, dto.Address);

                if (result == "Pomyślnie dodano kontrahenta")
                {
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, message = result });
            });

            // 4. USUWANIE KONTRAHENTA (API)
            app.MapDelete("/api/contractors/{id}", async (int id, HttpContext context, AppDbContext db, ContractorService contractorService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                // POPRAWKA: Sprawdzamy, czy CompanyId nie jest nullem
                if (employee == null || employee.CompanyId == null) 
                    return Results.Json(new { success = false, message = "Brak autoryzacji lub nie przypisano do firmy." });

                // POPRAWKA: Używamy .Value
                var result = contractorService.DeleteContractor(id, employee.CompanyId.Value);

                if (result == "Pomyślnie usunięto kontrahenta")
                {
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, message = result });
            });
        }
    }
}