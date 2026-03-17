using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using ERP_System.Core.DBTables;

namespace ERP_System.Core
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<DBCompany> Companies { get; set; }
        public DbSet<DBEmployee> Employees { get; set; }
        public DbSet<DBTransactionCategories> Categories { get; set; }
        public DbSet<DBFinancialOperations> FinancialOperations { get; set; }
        public DbSet<DBRecurringOperations> RecurringOperations { get; set; }
        public DbSet<DBRole> Roles { get; set; }

        public DbSet<DBContractor> Contractors { get; set; }
        public DbSet<DBInvoice> Invoices { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<DBEmployee>()
                .HasOne(u => u.Company)
                .WithMany(h => h.Members)
                .HasForeignKey(u => u.CompanyId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<DBFinancialOperations>()
                .HasOne(a => a.RecurringOperation)
                .WithOne(b => b.Transaction)
                .HasForeignKey<DBRecurringOperations>(b => b.TransactionPatternId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<DBInvoice>()
                .HasOne(i => i.Contractor)
                .WithMany()
                .HasForeignKey(i => i.ContractorId)
                .OnDelete(DeleteBehavior.Restrict);

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