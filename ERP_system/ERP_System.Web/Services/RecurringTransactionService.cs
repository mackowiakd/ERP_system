using ERP_System.Core;
using ERP_System.Core.DBTables;
using ERP_System.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Web.Services
{
    public class RecurringTransactionWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<RecurringTransactionWorker> _logger;

        public RecurringTransactionWorker(IServiceScopeFactory scopeFactory, ILogger<RecurringTransactionWorker> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                        // 1. Pobierz definicje, których czas nadszedł (NextRunDate <= Teraz) i są aktywne
                        // Include(t => t.Transaction) jest kluczowe, żeby pobrać dane wzorca (kwotę, kategorię itp.)
                        var tasksToRun = await db.RecurringOperations
                            .Include(rt => rt.Transaction)
                            .Where(rt => rt.IsActive && rt.NextRunDate <= DateTime.Now)
                            .ToListAsync(stoppingToken);

                        foreach (var rule in tasksToRun)
                        {
                            if (rule.Transaction == null) continue; // Zabezpieczenie

                            // 2. Stwórz NOWĄ transakcję na podstawie WZORCA
                            var newTransaction = new DBFinancialOperations
                            {
                                CompanyId = rule.Transaction.CompanyId,
                                CategoryId = rule.Transaction.CategoryId,
                                EmployeeId = rule.Transaction.EmployeeId,
                                Value = rule.Transaction.Value,
                                TransactionType = rule.Transaction.TransactionType,
                                Title = rule.Transaction.Title,
                                Description = rule.Transaction.Description + " (Auto)",
                                Date = rule.NextRunDate, // Data transakcji to data planowana
                                IsRepeatable = false, // Nowa transakcja nie jest szablonem!
                                RecurringOperation = null
                            };

                            db.FinancialOperations.Add(newTransaction);

                            // 3. Oblicz następną datę wykonania
                            rule.NextRunDate = CalculateNextDate(rule.NextRunDate, rule.IntervalValue, (TransactionIntervalType)rule.IntervalType);
                            
                            _logger.LogInformation($"Wygenerowano transakcję cykliczną dla User ID: {rule.Transaction.CompanyId}");
                        }

                        if (tasksToRun.Any())
                        {
                            await db.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Błąd w workerze transakcji cyklicznych.");
                }

                // Sprawdzaj co godzinę
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private DateTime CalculateNextDate(DateTime current, int value, TransactionIntervalType type)
        {
            return type switch
            {
                TransactionIntervalType.Days => current.AddDays(value),
                TransactionIntervalType.Weeks => current.AddDays(value * 7),
                TransactionIntervalType.Months => current.AddMonths(value),
                TransactionIntervalType.Years => current.AddYears(value),
                _ => current.AddMonths(1)
            };
        }
    }
}