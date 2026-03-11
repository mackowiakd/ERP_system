using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HomeBudgetManager.Core.DBTables;

namespace HomeBudgetManager.Core
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DBCompany> Company { get; set; }
        public DbSet<DBFinancialOperations> Transactions { get; set; }
        public DbSet<DBTransactionCategories> Categories { get; set; }
        public DbSet<DBEmployee> Users { get; set; }
        public DbSet<DBRole> Roles { get; set; }

        public DbSet<DBRecurringOperations> RepetableTransactions { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DBEmployee>()
                .HasOne<DBCompany>() // Fix: Specify the related entity type, not the foreign key property type
                .WithMany(h => h.Members)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DBFinancialOperations>()
                .HasOne(a => a.RepetableTransaction)
                .WithOne()
                .HasForeignKey<DBRecurringOperations>(b => b.TransactionPatternId);

            // Seed Roles
            modelBuilder.Entity<DBRole>().HasData(
                Enum.GetValues(typeof(SystemRole))
                    .Cast<SystemRole>()
                    .Select(r => new DBRole
                    {
                        Id = r,
                        Name = r.ToString()
                    })
            );
        }

    }
}
