using System;
using System.Collections.Generic;
using System.Linq;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Core
{
    public class InvoiceService
    {
        private readonly AppDbContext _db;

        public InvoiceService(AppDbContext db)
        {
            _db = db;
        }

        // 1. POBIERANIE LISTY FAKTUR Z DANEJ FIRMY
        public List<DBInvoice> GetCompanyInvoices(int companyId)
        {
            return _db.Invoices
                      .Include(i => i.Contractor) // Dociągamy dane kontrahenta (żeby mieć jego nazwę)
                      .Where(i => i.CompanyId == companyId)
                      .OrderByDescending(i => i.IssueDate) // Sortujemy od najnowszych
                      .ToList();
        }

        public string AddInvoice(int companyId, int contractorId, string invoiceNumber, 
                                 DateTime issueDate, DateTime dueDate, PaymentMethod paymentMethod, 
                                 decimal totalNet, decimal totalGross, InvoiceType type, string notes, InvoiceStatus status,
                                 bool isRecurring = false, int? frequencyUnit = null, int? intervalValue = null)
        {
            try
            {
                if (totalNet < 0 || totalGross < 0)
                    return "Błąd: Kwoty netto i brutto nie mogą być ujemne!";

                if (dueDate.Date < issueDate.Date)
                    return "Błąd: Termin płatności nie może być wcześniejszy niż data wystawienia!";

                if (string.IsNullOrWhiteSpace(invoiceNumber))
                    return "Błąd: Numer faktury nie może być pusty!";

                if (contractorId <= 0)
                    return "Błąd: Należy wybrać prawidłowego kontrahenta!";
                
                var newInvoice = new DBInvoice
                {
                    CompanyId = companyId,
                    ContractorId = contractorId,
                    InvoiceNumber = invoiceNumber,
                    IssueDate = issueDate,
                    DueDate = dueDate,
                    PaymentMethod = paymentMethod,
                    TotalNet = totalNet,
                    TotalGross = totalGross,
                    Type = type,
                    Notes = notes,
                    Status = status
                };

                _db.Invoices.Add(newInvoice);
                _db.SaveChanges();

                if (isRecurring && frequencyUnit.HasValue && intervalValue.HasValue)
                {
                    var nextRunDate = issueDate;
                    // Obliczamy pierwszą przyszłą datę
                    nextRunDate = CalculateNextDate(nextRunDate, intervalValue.Value, (ERP_System.Core.Enums.TransactionIntervalType)frequencyUnit.Value);

                    var recurringOp = new DBRecurringOperations
                    {
                        InvoiceId = newInvoice.Id,
                        IntervalValue = intervalValue.Value,
                        IntervalType = frequencyUnit.Value,
                        NextRunDate = nextRunDate,
                        IsActive = true
                    };
                    _db.RecurringOperations.Add(recurringOp);
                    _db.SaveChanges();
                }

                return "Pomyślnie dodano fakturę";
            }
            catch (Exception ex)
            {
                return $"Błąd podczas dodawania faktury: {ex.Message}";
            }
        }

        private DateTime CalculateNextDate(DateTime current, int value, ERP_System.Core.Enums.TransactionIntervalType type)
        {
            return type switch
            {
                ERP_System.Core.Enums.TransactionIntervalType.Days => current.AddDays(value),
                ERP_System.Core.Enums.TransactionIntervalType.Weeks => current.AddDays(value * 7),
                ERP_System.Core.Enums.TransactionIntervalType.Months => current.AddMonths(value),
                ERP_System.Core.Enums.TransactionIntervalType.Years => current.AddYears(value),
                _ => current.AddMonths(1)
            };
        }

        // 3. USUWANIE FAKTURY
        public string DeleteInvoice(int invoiceId, int companyId)
        {
            // Szukamy faktury, upewniając się, że należy do naszej firmy
            var invoice = _db.Invoices.FirstOrDefault(i => i.Id == invoiceId && i.CompanyId == companyId);

            if (invoice == null)
            {
                return "Nie znaleziono faktury lub brak uprawnień.";
            }

            try
            {
                _db.Invoices.Remove(invoice);
                _db.SaveChanges();
                return "Pomyślnie usunięto fakturę";
            }
            catch (Exception ex)
            {
                return $"Błąd podczas usuwania: {ex.Message}";
            }
        }

        // 4. GENEROWANIE HTML DLA PULPITU (Dashboard)
        public System.Text.StringBuilder ListInvoicesForDashboard(int companyId, int count = 8)
        {
            var invoices = _db.Invoices
                              .Include(i => i.Contractor)
                              .Where(i => i.CompanyId == companyId)
                              .OrderByDescending(i => i.IssueDate)
                              .Take(count)
                              .ToList();

            var sb = new System.Text.StringBuilder();

            foreach (var inv in invoices)
            {
                string date = inv.IssueDate.ToString("dd.MM.yyyy");
                string amount = inv.TotalGross.ToString("C2", new System.Globalization.CultureInfo("pl-PL"));
                // Dla faktur kosztowych (zakupowych) dajemy czerwony kolor, dla sprzedażowych zielony
                string colorClass = inv.Type == InvoiceType.Cost ? "amount-expense" : "amount-income";
                string contractor = inv.Contractor?.Name ?? "Nieznany";
                
                sb.Append($"""
                    <li class="transaction-item" onclick="window.location.href='/invoices'" style="cursor: pointer;">
                        <div class="transaction-info">
                             <div class="transaction-title">{inv.InvoiceNumber}</div>
                             <div class="transaction-details-sub">
                                <span class="category-badge">{contractor}</span>
                                <span class="transaction-date">{date}</span>
                             </div>
                        </div>
                        <div class="transaction-amount {colorClass}">
                            {amount}
                        </div>
                    </li>
                 """);
            }

            if (invoices.Count == 0)
            {
                sb.Append("<li class='transaction-item'><div class='transaction-main'><span>Brak faktur w systemie.</span></div></li>");
            }

            return sb;
        }

        // 5. EDYTOWANIE ISTNIEJĄCEJ FAKTURY
        public string EditInvoice(int invoiceId, string invoiceNumber,
                                  DateTime issueDate,
                                  decimal totalNet, decimal totalGross, InvoiceType type, string notes, InvoiceStatus status)
        {
            var invoice = _db.Invoices.FirstOrDefault(i => i.Id == invoiceId);
            if (invoice == null)
            {
                return "Nie znaleziono faktury lub brak uprawnień.";
            }
            try
            {
                invoice.InvoiceNumber = invoiceNumber;
                invoice.IssueDate = issueDate;
                invoice.TotalNet = totalNet;
                invoice.TotalGross = totalGross;
                invoice.Type = type;
                invoice.Notes = notes;
                invoice.Status = status;
                _db.SaveChanges();
                return "Pomyślnie zaktualizowano fakturę";
            }
            catch (Exception ex)
            {
                return $"Błąd podczas aktualizacji faktury: {ex.Message}";
            }
        }

    }
}