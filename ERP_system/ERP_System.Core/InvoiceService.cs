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

        // 2. DODAWANIE NOWEJ FAKTURY
        public string AddInvoice(int companyId, int contractorId, string invoiceNumber, 
                                 DateTime issueDate, DateTime dueDate, PaymentMethod paymentMethod, 
                                 decimal totalNet, decimal totalGross, InvoiceType type, InvoiceStatus status)
        {
            try
            {
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
                    Status = status
                };

                _db.Invoices.Add(newInvoice);
                _db.SaveChanges();

                return "Pomyślnie dodano fakturę";
            }
            catch (Exception ex)
            {
                return $"Błąd podczas dodawania faktury: {ex.Message}";
            }
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
    }
}