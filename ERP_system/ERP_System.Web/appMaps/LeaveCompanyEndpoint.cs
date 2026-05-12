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
            app.MapPost("/leave-company", async (HttpContext context, AppDbContext db) =>
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

                // load company
                var company = await db.Companies.FirstOrDefaultAsync(h => h.Id == user.CompanyId);
                if (company == null)
                {
                    return Results.Content("<div class='error'>Firma nie istnieje.</div>", "text/html");
                }

                // check if user is admin
                if (user.Id == company.CompanyAdminId)
                {
                    try 
                    {
                        // Deleting all company related data

                        var houseTransactions = await db.FinancialOperations
                            .Where(t => t.CompanyId == company.Id).ToListAsync();
                        
                        var recurringOps = await db.RecurringOperations
                            .Where(ro => db.FinancialOperations.Any(t => t.Id == ro.TransactionPatternId && t.CompanyId == company.Id))
                            .ToListAsync();
                        db.RecurringOperations.RemoveRange(recurringOps);
                        db.FinancialOperations.RemoveRange(houseTransactions);

                        var invoices = await db.Invoices
                            .Where(i => i.CompanyId == company.Id).ToListAsync();
                        db.Invoices.RemoveRange(invoices);

                        var contractors = await db.Contractors
                            .Where(c => c.CompanyId == company.Id).ToListAsync();
                        db.Contractors.RemoveRange(contractors);

                        var categories = await db.Categories
                            .Where(cat => cat.CompanyId == company.Id).ToListAsync();
                        db.Categories.RemoveRange(categories);

                        // reset members data
                        var members = await db.Employees.Where(u => u.CompanyId == company.Id).ToListAsync();
                        foreach (var member in members)
                        {
                            member.CompanyId = null;
                            if (member.Role != SystemRole.SystemAdmin)
                            {
                                member.Role = SystemRole.Guest;
                            }
                        }

                        // delete old company
                        db.Companies.Remove(company);
                        
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

                    // if company empty -> delete it
                    bool anyLeft = await db.Employees.AnyAsync(u => u.CompanyId == houseId);
                    if (!anyLeft)
                    {
                        db.Companies.Remove(company);
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