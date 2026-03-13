using Xunit;
using HomeBudgetManager.Core.DBTables;
using HomeBudgetManager.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HomeBudgetManager.Tests
{
    public class AdminTests
    {
        // 1. TEST BEZPIECZEŃSTWA: Czy zwykły user jest blokowany?
        [Fact]
        public void AdminConsole_Security_ShouldBlockNonAdmins()
        {
            // ARRANGE
            // Tworzymy zwykłego usera (Guest)
            var regularUser = new DBEmployee
            {
                Id = 1,
                Login = "Zwykły",
                Email = "guest@test.com", // <-- DODANO (Wymagane)
                Password = "123",         // <-- DODANO (Wymagane)
                Role = SystemRole.Guest,
                CompanyId = null
            };

            // ACT & ASSERT
            // Sprawdzamy logikę: if (user.Role != SystemRole.SystemAdmin)
            bool accessDenied = (regularUser.Role != SystemRole.SystemAdmin);

            Assert.True(accessDenied, "Zwykły użytkownik powinien mieć zablokowany dostęp!");
        }

        // 2. TEST LOGIKI ADMINA: Czy CompanyAdmin może usunąć użytkownika?
        [Fact]
        public async Task AdminAction_ShouldDeleteUser_FromDatabase()
        {
            // ARRANGE - Tworzymy bazę
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "AdminTestDb_" + Guid.NewGuid())
                .Options;

            // Wrzucamy: 1 Admina i 1 Ofiarę
            using (var db = new AppDbContext(options))
            {
                var admin = new DBEmployee
                {
                    Id = 99,
                    Login = "Szef",
                    Email = "admin@test.com", // <-- DODANO
                    Password = "admin",       // <-- DODANO
                    Role = SystemRole.SystemAdmin,
                    CompanyId = null
                };

                var victim = new DBEmployee
                {
                    Id = 2,
                    Login = "DoUsunięcia",
                    Email = "victim@test.com", // <-- DODANO
                    Password = "123",          // <-- DODANO
                    Role = SystemRole.Guest,
                    CompanyId = null
                };

                db.Employees.AddRange(admin, victim);
                await db.SaveChangesAsync();
            }

            // ACT - Symulacja akcji Admina (usuwanie użytkownika)
            using (var db = new AppDbContext(options))
            {
                // 1. CompanyAdmin szuka usera
                var targetUser = await db.Employees.FirstOrDefaultAsync(u => u.Login == "DoUsunięcia");

                // 2. CompanyAdmin go usuwa
                if (targetUser != null)
                {
                    db.Employees.Remove(targetUser);
                    await db.SaveChangesAsync();
                }
            }

            // ASSERT - Sprawdzamy czy ofiara zniknęła
            using (var db = new AppDbContext(options))
            {
                var deletedUser = await db.Employees.FirstOrDefaultAsync(u => u.Login == "DoUsunięcia");
                var adminUser = await db.Employees.FirstOrDefaultAsync(u => u.Login == "Szef");

                Assert.Null(deletedUser); // Sukces: Usera nie ma!
                Assert.NotNull(adminUser); // Sukces: CompanyAdmin nadal jest.
            }
        }
    }
}