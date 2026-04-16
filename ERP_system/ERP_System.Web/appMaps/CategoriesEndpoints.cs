using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Web.appMaps
{
    public class CategoriesEndpoints : IEndpoint
    {
        public record CreateCategoryDto(string Name, string? Description);

        public void Map(IEndpointRouteBuilder app)
        {

            app.MapGet("/categories/data", async (HttpContext context, AppDbContext db, CategoryService categoryService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null) return Results.Json(new List<object>());

                var categories = categoryService.listAllCompanyCategories(employee.CompanyId);
                var result = categories.Select(c => new
                {
                    id = c.Id,
                    name = c.Name,
                    description = c.Description,
                    companyId = c.CompanyId
                });
                return Results.Json(result);
            });

            app.MapGet("/categories/list", async (HttpContext context, AppDbContext db, CategoryService categoryService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (user == null)
                {
                    return Results.Content("<div class='error'>Błąd: Użytkownik nieznaleziony.</div>", "text/html");
                }

                var categories = categoryService.listAllCompanyCategories(user.CompanyId);

                // 3. Zbuduj HTML
                var htmlBuilder = new System.Text.StringBuilder();
                htmlBuilder.Append("<select id='category' name='categoryId' required class='form-input' onchange='handleCategoryChange(this)'>");

                htmlBuilder.Append("<option value=''>Wybierz kategorię</option>");
                foreach (var cat in categories)
                {
                    htmlBuilder.Append($"<option value='{cat.Id}'>{cat.Name}</option>");
                }

                htmlBuilder.Append("<option value='new-category'>Dodaj kategorię</option>");
                htmlBuilder.Append("</select>");

                return Results.Content(htmlBuilder.ToString(), "text/html");
            });

            app.MapPost("/categories/add", async (CreateCategoryDto dto, HttpContext context, AppDbContext db, CategoryService catService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (user == null)
                {
                    return Results.Content("<div class='error'>Błąd: Użytkownik nieznaleziony.</div>", "text/html");
                }

                if (user.CompanyId == null || user.CompanyId == 0) 
                {
                    return Results.Json(new { 
                        success = false, 
                        requireCompany = true, 
                        message = "Aby dodać kategorię, musisz najpierw dołączyć do firmy lub założyć nową." 
                    });
                }

                if (string.IsNullOrWhiteSpace(dto.Name))
                    return Results.Json(new { success = false, message = "Nazwa wymagana" });

                var result = catService.addCategory(user.Id, dto.Name, dto.Description);

                if (result == "Poprawnie dodano kategorię")
                {
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, message = result });
            });

            app.MapDelete("/categories/delete/{id}", async (int id, HttpContext context, AppDbContext db, CategoryService catService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (user == null)
                    return Results.Json(new { success = false, message = "Użytkownik nieznaleziony" });

                var result = catService.deleteCategory(user.Id, id);
                if (result == "Pomyślnie usunięto kategorię")
                {
                    return Results.Json(new { success = true });
                }
                return Results.Json(new { success = false, message = result });
            });

            app.MapPut("/categories/update/{id}", async (int id, CreateCategoryDto dto, HttpContext context, AppDbContext db, CategoryService catService) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (user == null)
                    return Results.Json(new { success = false, message = "Użytkownik nieznaleziony" });

                var result = catService.modifyCategory(user.Id, id, dto.Name, dto.Description ?? "");
                if (result == "Pomyślnie zedytowano kategorię")
                {
                    return Results.Json(new { success = true });
                }
                return Results.Json(new { success = false, message = result });
            });
        }
    }
}
