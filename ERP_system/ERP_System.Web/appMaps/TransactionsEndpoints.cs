using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Web.appMaps
{
    public class TransactionsEndpoints : IEndpoint
    {
        public void Map(IEndpointRouteBuilder app)
        {
            // Delete transaction or invoice (used by calendar)
            app.MapDelete("/transactions", async (string id, HttpContext context, AppDbContext db, TransactionService transactionService, InvoiceService invoiceService) =>
            {
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

                var user = await db.Employees.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null) return Results.Unauthorized();

                try
                {
                    if (id.StartsWith("trans_"))
                    {
                        int transId = int.Parse(id.Substring(6));
                        transactionService.deleteTransaction(transId, userId);
                    }
                    else if (id.StartsWith("rec_"))
                    {
                        return Results.Json(new { success = false, message = "Nie można usunąć prognozowanej transakcji. Zatrzymaj cykl we właściwej transakcji." });
                    }
                    else if (int.TryParse(id, out int invoiceId))
                    {
                        invoiceService.DeleteInvoice(invoiceId, user.CompanyId ?? 0);
                    }
                    else
                    {
                        return Results.Json(new { success = false, message = "Nieznany format ID transakcji." });
                    }
                    
                    return Results.Ok(new { success = true });
                }
                catch (Exception ex)
                {
                    return Results.Json(new { success = false, message = ex.Message });
                }
            });

            // Stop recurrence for transaction
            app.MapPost("/api/transactions/stop-recurring/{id}", async (string id, HttpContext context, AppDbContext db) =>
            {
                if (!context.Request.Cookies.TryGetValue("user_id", out var userIdStr) || !int.TryParse(userIdStr, out int userId))
                    return Results.Unauthorized();

                int transId;
                if (id.StartsWith("trans_")) transId = int.Parse(id.Substring(6));
                else if (int.TryParse(id, out int val)) transId = val;
                else return Results.Json(new { success = false, message = "Nieprawidłowe ID." });

                var transaction = await db.FinancialOperations
                    .Include(t => t.RecurringOperation)
                    .FirstOrDefaultAsync(t => t.Id == transId && t.EmployeeId == userId);

                if (transaction == null) return Results.NotFound();

                if (transaction.RecurringOperation != null)
                {
                    db.RecurringOperations.Remove(transaction.RecurringOperation);
                    transaction.IsRepeatable = false;
                    await db.SaveChangesAsync();
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, message = "Ta transakcja nie jest cykliczna." });
            });
            
             // Stop recurrence for invoice
            app.MapPost("/api/invoices/stop-recurring/{id}", async (string id, HttpContext context, AppDbContext db) =>
            {
                var loginUser = context.Request.Cookies["logged_user"];
                var employee = await db.Employees.FirstOrDefaultAsync(u => u.Login == loginUser);

                if (employee == null || employee.CompanyId == null) 
                    return Results.Unauthorized();

                if (!int.TryParse(id, out int invoiceId))
                     return Results.Json(new { success = false, message = "Nieprawidłowe ID faktury." });

                var invoice = await db.Invoices
                    .Include(i => i.RecurringOperation)
                    .FirstOrDefaultAsync(i => i.Id == invoiceId && i.CompanyId == employee.CompanyId);

                if (invoice == null) return Results.NotFound();

                if (invoice.RecurringOperation != null)
                {
                    db.RecurringOperations.Remove(invoice.RecurringOperation);
                    await db.SaveChangesAsync();
                    return Results.Json(new { success = true });
                }

                return Results.Json(new { success = false, message = "Ta faktura nie jest cykliczna." });
            });
        }
    }
}
