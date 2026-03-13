using HomeBudgetManager.Core;
using HomeBudgetManager.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace HomeBudgetManager.Web.appMaps
{
    public class LeaveHouseholdEndpoint : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            app.MapPost("/leave-household", async (HttpContext context, AppDbContext db) =>
            {
                // load user
                var login = context.Request.Cookies["logged_user"];
                if (string.IsNullOrEmpty(login))
                {
                    return Results.Content("<div class='error'>Błąd: użytkownik niezalogowany.</div>", "text/html");
                }

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Login == login);
                if (user == null)
                {
                    return Results.Content("<div class='error'>Błąd: użytkownik nie istnieje.</div>", "text/html");
                }
                if (user.CompanyId == null)
                {
                    return Results.Content("<div class='error'>Nie należysz do żadnego domostwa.</div>", "text/html");
                }

                // load household
                var house = await db.Companies.FirstOrDefaultAsync(h => h.Id == user.CompanyId);
                if (house == null)
                {
                    return Results.Content("<div class='error'>Domostwo nie istnieje.</div>", "text/html");
                }

                // check if user is admin
                if (user.Id == house.CompanyAdminId)
                {
                    // delete all transactions for this company
                    var houseTransactions = await db.FinancialOperations
                        .Where(t => t.CompanyId == house.Id)
                        .ToListAsync();

                    db.FinancialOperations.RemoveRange(houseTransactions);

                    // reset for all members
                    var members = await db.Employees.Where(u => u.CompanyId == house.Id).ToListAsync();
                    foreach (var member in members)
                    {
                        member.CompanyId = null;
                        if (member.Role != SystemRole.SystemAdmin)
                        {
                            member.Role = SystemRole.Guest;
                        }
                    }

                    // delete household
                    db.Companies.Remove(house);
                    await db.SaveChangesAsync();

                    return Results.Content(@"
                        <section class='card'>
                            <h2>Twoje domostwo</h2>
                            <div class='success'>Opuszczono domostwo (usunięto domostwo)!</div>
                            <p>Za chwilę nastąpi powrót do pulpitu...</p>
                        </section>
                        <script>
                            setTimeout(() => { window.location.href = '/dashboard'; }, 1200);
                        </script>
                    ", "text/html");

                }
                else
                {
                    // regular user leaves household
                    int houseId = user.CompanyId.Value;
                    user.CompanyId = null;
                    
                    if (user.Role != SystemRole.SystemAdmin)
                    {
                        user.Role = SystemRole.Guest;
                    }
                    
                    await db.SaveChangesAsync();

                    // if house empty -> delete it
                    bool anyLeft = await db.Employees.AnyAsync(u => u.CompanyId == houseId);
                    if (!anyLeft)
                    {
                        db.Companies.Remove(house);
                        await db.SaveChangesAsync();
                    }

                    return Results.Content(@"
                        <section class='card'>
                            <h2>Twoje domostwo</h2>
                            <div class='success'>Opuszczono domostwo!</div>
                            <p>Za chwilę nastąpi powrót do pulpitu...</p>
                        </section>
                        <script>
                            setTimeout(() => { window.location.href = '/dashboard'; }, 1200);
                        </script>
                    ", "text/html");

                }
            });
        }
    }
}
