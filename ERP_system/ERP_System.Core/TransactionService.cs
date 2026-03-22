using ERP_System.Core.DBTables;
using ERP_System.Core.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP_System.Core
{
    public class TransactionService
    {
        AppDbContext db;

        public TransactionService(AppDbContext db)
        {
            this.db = db;
        }

        public void addTransaction(DBFinancialOperations transaction)
        {
            if (transaction.CompanyId <= 0)                                                                                                                                                                      
            {                                                                                                                                                                                                    
                throw new InvalidOperationException("<div class='error'>Błąd: Musisz najpierw założyć firmę lub do niej dołączyć, aby dodać fakturę!</div>");                                                    
            } 
            try
            {
                db.Add(transaction);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("<div class='error'>Błąd podczas zapisywania transakcji: " + ex.Message + "</div>");   
            }
        }

        // Zmieniono parametr z houseId na companyId
        public void addTransaction(int userId, int categoryId, decimal value, TransactionType type, DateTime date, bool isRepeatable, int? transactionInterval, string title, string? description, int? companyId, int? frequencyUnit)
        {
            if (companyId == null || companyId == 0)                                                                                                                                                             
            {                                                                                                                                                                                                    
                throw new InvalidOperationException("<div class='error'>Błąd: Musisz najpierw założyć firmę lub do niej dołączyć, aby dodać fakturę!</div>");                                                    
            }  

            var newTransaction = new DBFinancialOperations
            {
                EmployeeId = userId,         
                CategoryId = categoryId,
                Value = value,
                TransactionType = type,
                Date = date,
                IsRepeatable = isRepeatable,
                Title = title,
                Description = description,
                CompanyId = companyId ?? 0   
            };
            db.Add(newTransaction);
            db.SaveChanges();

            if (isRepeatable && transactionInterval.HasValue && frequencyUnit.HasValue)
            {
                try
                {
                    DateTime nextRunDate = date;
                    var unit = (TransactionIntervalType)frequencyUnit.Value;

                    nextRunDate = unit switch
                    {
                        TransactionIntervalType.Days => date.AddDays(transactionInterval.Value),
                        TransactionIntervalType.Weeks => date.AddDays(transactionInterval.Value * 7),
                        TransactionIntervalType.Months => date.AddMonths(transactionInterval.Value),
                        TransactionIntervalType.Years => date.AddYears(transactionInterval.Value),
                        _ => date.AddMonths(transactionInterval.Value)
                    };

                    // POPRAWKA: Czysta encja cykliczna (bez dublowania tytułu i kategorii)
                    var newRepTransaction = new DBRecurringOperations
                    {
                        TransactionPatternId = newTransaction.Id,
                        IntervalValue = transactionInterval.Value,
                        IntervalType = frequencyUnit.Value,
                        NextRunDate = nextRunDate,
                        IsActive = true
                    };

                    db.Add(newRepTransaction);
                    db.SaveChanges();
                }
                catch
                {
                    throw new InvalidOperationException("<div class='error'>Błąd: nie dodano transakcji okresowej</div>");
                }
            }
        }

        public void editTransaction(int transactionId, int categoryId, decimal value, bool isRepeatable, string title, string? description, int? companyId)
        {
            var transaction = db.FinancialOperations.FirstOrDefault(t => t.Id == transactionId);

            if (transaction == null)
            {
                throw new ArgumentNullException("<div class='error'>Błąd: nie znaleziono transakcji po ID</div>");
            }

            if (description == null)
            {
                description = transaction.Description;
            }

            if (companyId.HasValue)
            {
                transaction.CompanyId = companyId.Value;
            }

            transaction.CategoryId = categoryId;
            transaction.Value = value;
            transaction.IsRepeatable = isRepeatable;
            transaction.Title = title;
            transaction.Description = description;
            db.SaveChanges();
        }

        public void deleteTransaction(int transactionId, int userId)
        {
            // POPRAWKA: t.EmployeeId
            var transaction = db.FinancialOperations.FirstOrDefault(t => t.Id == transactionId && t.EmployeeId == userId);

            if (transaction == null)
            {
                throw new ArgumentNullException("<div class='error'>Błąd: nie znaleziono transakcji po ID</div>");
            }

            try
            {
                db.Remove(transaction);
                db.SaveChanges();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("<div class='error'>" + ex.Message + "</div>");
            }
        }

        public StringBuilder listTransactionsForDashboard(List<DBFinancialOperations> transactions)
        {
            var sb = new System.Text.StringBuilder();

            foreach (var t in transactions)
            {
                string date = t.Date.ToString("yyyy-MM-dd");
                string displayDate = t.Date.ToString("dd.MM.yyyy");
                string amount = t.Value.ToString("C2", new System.Globalization.CultureInfo("pl-PL"));
                string colorClass = t.Value < 0 ? "amount-expense" : "amount-income";
                var category = db.Categories.FirstOrDefault(c => c.Id == t.CategoryId);

                string safeDescription = (t.Description ?? "").Replace("\"", "&quot;").Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");
                string safeTitle = (t.Title ?? "Bez tytułu").Replace("\"", "&quot;").Replace("'", "\\'").Replace("\r", "").Replace("\n", " ");

                sb.Append($"""
                    <li class="transaction-item" onclick="openDashboardTransactionDetails({t.Id}, '{t.Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}', '{safeTitle}', '{safeDescription}', '{date}')" style="cursor: pointer;">
                        <div class="transaction-info">
                             <div class="transaction-title">{safeTitle}</div>
                             <div class="transaction-details-sub">
                                <span class="category-badge">{category?.Name ?? "Brak"}</span>
                                <span class="transaction-date">{displayDate}</span>
                             </div>
                        </div>
                        <div class="transaction-amount {colorClass}">
                            {amount}
                        </div>
                    </li>
                 """);
            }

            return sb;
        }

        public List<DBFinancialOperations> AllUserTransactions(int userId)
        {
            // POPRAWKA: t.EmployeeId zamians CompanyId/UserId
            return db.FinancialOperations.Where(t => t.EmployeeId == userId).OrderByDescending(t => t.Date).ToList();
        }

        public List<DBFinancialOperations> SomeUserTransactions(int userId, int amount)
        {
            // POPRAWKA: t.EmployeeId
            var recurringRules = db.RecurringOperations
            .Include(rt => rt.Transaction)
            .ThenInclude(t => t.Category) // Pobieramy kategorię z podpiętej operacji!
            .Where(rt => rt.Transaction != null && userId == rt.Transaction.EmployeeId && rt.IsActive).ToList();
            var temp = db.FinancialOperations.Where(t => t.EmployeeId == userId && t.Date <= DateTime.Now).OrderByDescending(t => t.Date).Take(amount).ToList();
            DateTime now = DateTime.Now;
            // 3. Project Future Transactions
            foreach (var rule in recurringRules)
            {
                var currentDate = rule.NextRunDate;
                var unit = (TransactionIntervalType)rule.IntervalType; // ZMIANA z FrequencyUnit
                var occurenceNumber = 1;
                // Loop to find all occurrences within the requested range
                while (currentDate <= DateTime.Now)
                {
                    if (currentDate >= new DateTime(now.Year, now.Month, 1))
                    {
                        // Create a transient transaction object for calculation
                        var projected = new DBFinancialOperations
                        {
                            Id = 0, // transient
                            CompanyId = rule.Transaction!.CompanyId,   // POPRAWKA
                            EmployeeId = rule.Transaction.EmployeeId, // POPRAWKA
                            CategoryId = rule.Transaction.CategoryId,
                            Category = rule.Transaction.Category,
                            Value = rule.Transaction.Value,           // POPRAWKA: Pobieramy kwotę z transakcji, nie z reguły
                            Title = "Projected",
                            TransactionType = rule.Transaction.TransactionType,
                            Date = currentDate,
                            IsRepeatable = false
                        };

                        occurenceNumber++;
                    }

                    // Advance to next occurrence (ZMIANA z TransactionInterval na IntervalValue)
                    currentDate = unit switch
                    {
                        TransactionIntervalType.Days => currentDate.AddDays(rule.IntervalValue),
                        TransactionIntervalType.Weeks => currentDate.AddDays(rule.IntervalValue * 7),
                        TransactionIntervalType.Months => currentDate.AddMonths(rule.IntervalValue),
                        TransactionIntervalType.Years => currentDate.AddYears(rule.IntervalValue),
                        _ => currentDate.AddMonths(1)
                    };
                }
                //No more occurences
                foreach (var transaction in temp)
                {
                    if (transaction.Id == rule.TransactionPatternId) //Matches by ID
                    {
                        transaction.Value = transaction.Value * occurenceNumber; //Multiply to accomodate multiple occurences in one show
                        break; //No use looping more
                    }
                }
            }
            return temp;
            //return db.FinancialOperations.Where(t => t.EmployeeId == userId && t.Date <= DateTime.Now).OrderByDescending(t => t.Date).Take(amount).ToList();
        }

        public List<DBFinancialOperations> allCompanyTransactions(int companyId)
        {
            return db.FinancialOperations.Where(t => t.CompanyId == companyId).OrderByDescending(t => t.Date).ToList();
        }

        public List<DBFinancialOperations> someCompanyTransactions(int companyId, int amount)
        {
            return db.FinancialOperations.Where(t => t.CompanyId == companyId).OrderByDescending(t => t.Date).Take(amount).ToList();
        }
    }
}