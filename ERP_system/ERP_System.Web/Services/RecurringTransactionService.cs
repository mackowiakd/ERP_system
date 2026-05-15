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

                        // Load definitions with NextRunDate <= Now which are also active
                        var tasksToRun = await db.RecurringOperations
                            .Include(rt => rt.Invoice)
                            .Include(rt => rt.BaseInvoice)
                            .Where(rt => rt.IsActive && rt.NextRunDate <= DateTime.Now)
                            .ToListAsync(stoppingToken);

                        foreach (var rule in tasksToRun)
                        {
                            while (rule.NextRunDate <= DateTime.Now && rule.IsActive)
                            {
                                if (rule.Invoice != null)
                                {
                                    var newTransaction = new DBFinancialOperations
                                    {
                                        CompanyId = rule.Invoice.CompanyId,
                                        CategoryId = rule.Invoice.CategoryId,
                                        EmployeeId = rule.Invoice.EmployeeId,
                                        Value = rule.Invoice.Value,
                                        TransactionType = rule.Invoice.TransactionType,
                                        Title = rule.Invoice.Title,
                                        Description = rule.Invoice.Description + " (Auto)",
                                        Date = rule.NextRunDate,
                                        IsRepeatable = false
                                    };
                                    db.FinancialOperations.Add(newTransaction);
                                }
                                else if (rule.BaseInvoice != null)
                                {
                                    var newInvoice = new DBInvoice
                                    {
                                        CompanyId = rule.BaseInvoice.CompanyId,
                                        ContractorId = rule.BaseInvoice.ContractorId,
                                        InvoiceNumber = rule.BaseInvoice.InvoiceNumber + " (C)",
                                        IssueDate = rule.NextRunDate,
                                        DueDate = rule.NextRunDate.AddDays(14),
                                        PaymentMethod = rule.BaseInvoice.PaymentMethod,
                                        TotalNet = rule.BaseInvoice.TotalNet,
                                        TotalGross = rule.BaseInvoice.TotalGross,
                                        Type = rule.BaseInvoice.Type,
                                        Notes = rule.BaseInvoice.Notes,
                                        Status = InvoiceStatus.Unpaid,
                                        CategoryId = rule.BaseInvoice.CategoryId
                                    };
                                    db.Invoices.Add(newInvoice);
                                }
                                else
                                {
                                    break;
                                }

                                rule.NextRunDate = CalculateNextDate(rule.NextRunDate, rule.IntervalValue, (TransactionIntervalType)rule.IntervalType);
                                _logger.LogInformation($"Wygenerowano transakcję/fakturę cykliczną dla daty {rule.NextRunDate}");
                            }
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

                // Check each hour
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