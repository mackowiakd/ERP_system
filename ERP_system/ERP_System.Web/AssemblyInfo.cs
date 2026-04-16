using ERP_System.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ERP_System.Web
{
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

            optionsBuilder.UseSqlite("Data Source=HbmLocal.db", b => b.MigrationsAssembly("ERP_System.Core"));

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}