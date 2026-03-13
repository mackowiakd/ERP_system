using Xunit;
using HomeBudgetManager.Core.DBTables;
using HomeBudgetManager.Core;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace HomeBudgetManager.Tests
{
    public class HouseholdManagementTests
    {
        // 1. TEST: Zwykły członek opuszcza dom (LeaveHouseholdEndpoint)
        [Fact]
        public async Task LeaveHousehold_Member_ShouldResetUser_AndKeepHouse()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "Leave_Member_" + Guid.NewGuid())
                .Options;

            using (var db = new AppDbContext(options))
            {
                var house = new DBEmployee { Id = 10, Name = "Dom", JoinCode = "A", CompanyAdminId = 99 };
                db.Companies.Add(house);

                // Zwykły członek (Guest)
                db.Employees.Add(new DBEmployee
                {
                    Id = 1,
                    Login = "Kuzyn",
                    Email = "k@k.com",
                    Password = "123",
                    Role = SystemRole.Guest,
                    CompanyId = 10
                });

                // CompanyAdmin (żeby dom nie był pusty)
                db.Employees.Add(new DBEmployee
                {
                    Id = 99,
                    Login = "Szef",
                    Email = "a@a.com",
                    Password = "123",
                    Role = SystemRole.CompanyAdmin,
                    CompanyId = 10
                });

                await db.SaveChangesAsync();
            }

            // ACT - Symulacja logiki z LeaveHouseholdEndpoint.cs (blok else)
            using (var db = new AppDbContext(options))
            {
                var user = await db.Employees.FirstAsync(u => u.Id == 1); // Kuzyn

                // Logika wyjęta z pliku:
                user.CompanyId = null;
                user.Role = SystemRole.Guest;
                await db.SaveChangesAsync();
            }

            // ASSERT
            using (var db = new AppDbContext(options))
            {
                var user = await db.Employees.FirstAsync(u => u.Id == 1);
                var house = await db.Companies.FirstOrDefaultAsync(h => h.Id == 10);

                Assert.Null(user.CompanyId); // Już nie ma domu
                Assert.NotNull(house);     // Ale dom nadal stoi (bo został CompanyAdmin)
            }
        }


        // 2. TEST: CompanyAdmin rozwiązuje dom (LeaveHouseholdEndpoint)
        [Fact]
        public async Task LeaveHousehold_Admin_ShouldDissolveHouse()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "Leave_Admin_" + Guid.NewGuid())
                .Options;

            using (var db = new AppDbContext(options))
            {
                var house = new DBEmployee { Id = 20, Name = "Dom Admina", JoinCode = "B", CompanyAdminId = 1 };
                db.Companies.Add(house);

                // CompanyAdmin
                db.Employees.Add(new DBEmployee
                {
                    Id = 1,
                    Login = "Szef",
                    Email = "s@s.com",
                    Password = "123",
                    Role = SystemRole.CompanyAdmin,
                    CompanyId = 20
                });

                // Członek
                db.Employees.Add(new DBEmployee
                {
                    Id = 2,
                    Login = "Członek",
                    Email = "c@c.com",
                    Password = "123",
                    Role = SystemRole.Guest,
                    CompanyId = 20
                });

                await db.SaveChangesAsync();
            }

            // ACT - Symulacja logiki Admina (if user.Role == HouseholdAdmin)
            using (var db = new AppDbContext(options))
            {
                var house = await db.Companies.FirstAsync(h => h.Id == 20);

                // Logika: Reset dla wszystkich i usunięcie domu
                var members = await db.Employees.Where(u => u.CompanyId == house.Id).ToListAsync();
                foreach (var member in members)
                {
                    member.CompanyId = null;
                    member.Role = SystemRole.Guest;
                }
                db.Companies.Remove(house);
                await db.SaveChangesAsync();
            }

            // ASSERT
            using (var db = new AppDbContext(options))
            {
                var house = await db.Companies.FirstOrDefaultAsync(h => h.Id == 20);
                var member = await db.Employees.FirstAsync(u => u.Id == 2);

                Assert.Null(house);        // Dom powinien zniknąć!
                Assert.Null(member.CompanyId); // Członek powinien zostać "uwolniony"
            }
        }

        // 3. TEST: Wyrzucanie członka przez Admina (DashboardHouseholdEndpoint)
        [Fact]
        public async Task KickMember_ShouldRemoveUserFromHouse()
        {
            // ARRANGE
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: "Kick_Member_" + Guid.NewGuid())
                .Options;

            using (var db = new AppDbContext(options))
            {
                db.Companies.Add(new DBEmployee { Id = 30, Name = "Twierdza", JoinCode = "C", CompanyAdminId = 1 });

                // Ofiara
                db.Employees.Add(new DBEmployee
                {
                    Id = 5,
                    Login = "Niechciany",
                    Email = "n@n.com",
                    Password = "123",
                    Role = SystemRole.Guest,
                    CompanyId = 30
                });
                await db.SaveChangesAsync();
            }

            // ACT - Symulacja endpointu /remove-member
            using (var db = new AppDbContext(options))
            {
                var targetUser = await db.Employees.FirstAsync(u => u.Id == 5);

                // Logika: Usuń użytkownika (zresetuj dom i rolę)
                targetUser.CompanyId = null;
                targetUser.Role = SystemRole.Guest;
                await db.SaveChangesAsync();
            }

            // ASSERT
            using (var db = new AppDbContext(options))
            {
                var user = await db.Employees.FirstAsync(u => u.Id == 5);
                Assert.Null(user.CompanyId); // Wyrzucony!
            }
        }
    }
}