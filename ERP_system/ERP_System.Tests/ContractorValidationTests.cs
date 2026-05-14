using System;
using System.Linq;
using Xunit;
using ERP_System.Core;
using ERP_System.Core.DBTables;
using Microsoft.EntityFrameworkCore;

namespace ERP_System.Tests
{
    public class ContractorValidationTests
    {
        private AppDbContext GetInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            return new AppDbContext(options);
        }

        [Fact]
        public void AddContractor_InvalidNip_ShouldThrowException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new ContractorService(context);

            // Act & Assert
            // Próbujemy dodać NIP, który ma tylko 3 cyfry. Oczekujemy, że system rzuci wyjątek.
            var exception = Assert.Throws<Exception>(() => 
                service.AddContractor(1, "Firma Krzak", "123", "Polna 5", "Warszawa", "00-000", "biuro@firma.pl"));

            // Sprawdzamy, czy treść błędu jest dokładnie taka, jaką zaprogramowaliśmy
            Assert.Equal("NIP musi składać się z dokładnie 10 cyfr.", exception.Message);
        }

        [Fact]
        public void AddContractor_InvalidEmail_ShouldThrowException()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new ContractorService(context);

            // Act & Assert
            // Próbujemy dodać złego maila bez małpy
            var exception = Assert.Throws<Exception>(() => 
                service.AddContractor(1, "Firma Krzak", "1234567890", "Polna 5", "Warszawa", "00-000", "zly-adres-email.pl"));

            Assert.Equal("Podaj poprawny adres e-mail (np. biuro@firma.pl).", exception.Message);
        }

        [Fact]
        public void AddContractor_ValidData_ShouldSaveToDatabase()
        {
            // Arrange
            using var context = GetInMemoryDbContext();
            var service = new ContractorService(context);

            // Act
            // Idealne dane - NIP ma 10 cyfr, email ma małpę
            var contractor = service.AddContractor(1, "Idealna Firma", "1234567890", "Długa 10", "Kraków", "30-000", "kontakt@idealna.pl");

            // Assert
            var savedContractor = context.Contractors.FirstOrDefault(c => c.TaxId == "1234567890");
            Assert.NotNull(savedContractor);
            Assert.Equal("Idealna Firma", savedContractor.Name);
            Assert.Equal("kontakt@idealna.pl", savedContractor.Email);
        }
    }
}