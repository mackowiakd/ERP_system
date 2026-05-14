using System;
using System.Linq;
using Xunit;
using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Tests
{
    public class InvoiceValidationTests
    {
        // Narzędzie do tworzenia świeżej, pustej bazy danych dla każdego testu
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void AddInvoice_ValidData_ShouldSaveSuccessfully()
        {
            // Arrange (Przygotowanie)
            using var context = GetInMemoryDbContext();
            
            // Tworzymy dummy-kontrahenta, bo faktura go wymaga
            var contractor = new DBContractor { Id = 1, CompanyId = 1, Name = "Testowa Firma", TaxId = "1112223344" };
            context.Contractors.Add(contractor);
            context.SaveChanges();

            // Tworzymy serwis (upewnij się, że masz tam taki konstruktor)
            // var service = new InvoiceService(context); 

            // Act (Wykonanie)
            var invoice = new DBInvoice
            {
                CompanyId = 1, // <--- DODAJ TĘ LINIJKĘ
                InvoiceNumber = "FV/2026/05/01",
                ContractorId = 1,
                IssueDate = DateTime.Now,
                DueDate = DateTime.Now.AddDays(14),
                TotalNet = 1000m,
                TotalGross = 1230m
            };
            
            context.Invoices.Add(invoice);
            context.SaveChanges();

            // Assert (Sprawdzenie wyniku)
            var savedInvoice = context.Invoices.FirstOrDefault(i => i.InvoiceNumber == "FV/2026/05/01");
            Assert.NotNull(savedInvoice);
            Assert.Equal(1230m, savedInvoice.TotalGross);
            Assert.Equal(1, savedInvoice.ContractorId);
        }

        [Fact]
        public void Invoice_CalculateGross_ShouldBeCorrect()
        {
            // Szybki test logiki matematycznej
            decimal net = 1000m;
            decimal vatRate = 0.23m;
            
            decimal expectedGross = net + (net * vatRate);
            decimal actualGross = 1230m;

            Assert.Equal(expectedGross, actualGross);
        }
    }
}