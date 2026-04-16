using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;
using System;

namespace ERP_System.Web.appMaps
{
    public class LeaveCompanyEndpoint : IEndpoint
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
                    return Results.Content("<div class='error'>Nie należysz do żadnej firmy.</div>", "text/html");
                }

                // load household (company)
                var house = await db.Companies.FirstOrDefaultAsync(h => h.Id == user.CompanyId);
                if (house == null)
                {
                    return Results.Content("<div class='error'>Firma nie istnieje.</div>", "text/html");
                }

                // check if user is admin
                if (user.Id == house.CompanyAdminId)
                {
                    try 
                    {
                        // 1. Usuwamy wszystkie dane powiązane z firmą (czyszczenie kaskadowe ręczne)
                        
                        // Operacje finansowe
                        var houseTransactions = await db.FinancialOperations
                            .Where(t => t.CompanyId == house.Id).ToListAsync();
                        
                        // Operacje cykliczne powiązane z transakcjami tej firmy
                        var recurringOps = await db.RecurringOperations
                            .Where(ro => db.FinancialOperations.Any(t => t.Id == ro.TransactionPatternId && t.CompanyId == house.Id))
                            .ToListAsync();
                        db.RecurringOperations.RemoveRange(recurringOps);
                        db.FinancialOperations.RemoveRange(houseTransactions);

                        // Faktury
                        var invoices = await db.Invoices
                            .Where(i => i.CompanyId == house.Id).ToListAsync();
                        db.Invoices.RemoveRange(invoices);

                        // Kontrahenci
                        var contractors = await db.Contractors
                            .Where(c => c.CompanyId == house.Id).ToListAsync();
                        db.Contractors.RemoveRange(contractors);

                        // Kategorie transakcji
                        var categories = await db.Categories
                            .Where(cat => cat.CompanyId == house.Id).ToListAsync();
                        db.Categories.RemoveRange(categories);

                        // 2. Resetujemy dane użytkowników (członków firmy)
                        var members = await db.Employees.Where(u => u.CompanyId == house.Id).ToListAsync();
                        foreach (var member in members)
                        {
                            member.CompanyId = null;
                            if (member.Role != SystemRole.SystemAdmin)
                            {
                                member.Role = SystemRole.Guest;
                            }
                        }

                        // 3. Usuwamy samą firmę
                        db.Companies.Remove(house);
                        
                        await db.SaveChangesAsync();

                        return Results.Content(@"
                            <section class='card'>
                                <h2>Twoja firma</h2>
                                <div class='success'>Firma została całkowicie usunięta wraz z historią!</div>
                                <p>Za chwilę nastąpi powrót do pulpitu...</p>
                            </section>
                            <script>
                                setTimeout(() => { window.location.href = '/dashboard'; }, 1200);
                            </script>
                        ", "text/html");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Błąd podczas usuwania firmy: {ex.Message}");
                        return Results.Content($"<div class='error'>Błąd serwera: Nie udało się usunąć firmy. ({ex.Message})</div>", "text/html");
                    }
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
                            <h2>Twoja firma</h2>
                            <div class='success'>Opuszczono struktury firmy!</div>
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