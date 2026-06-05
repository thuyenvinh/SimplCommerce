using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using SimplCommerce.Infrastructure.Modules;
using SimplCommerce.Module.Core.Data;

namespace SimplCommerce.Migrations
{
    /// <summary>
    /// Design-time factory used by <c>dotnet ef migrations</c>. Seeds the module manifest so
    /// <see cref="SimplDbContext.OnModelCreating"/> can discover entities across every bundled
    /// module, then hands EF a DbContextOptions pointing at this assembly for the migration
    /// output folder. No live SQL Server is required — the connection string is a placeholder.
    /// </summary>
    public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SimplDbContext>
    {
        public SimplDbContext CreateDbContext(string[] args)
        {
            ModuleManifestLoader.LoadAllBundled();

            var options = new DbContextOptionsBuilder<SimplDbContext>()
                .UseSqlServer(
                    "Server=(localdb)\\mssqllocaldb;Database=SimplCommerce_DesignTime;Trusted_Connection=True;",
                    sql => sql.MigrationsAssembly(typeof(DesignTimeDbContextFactory).Assembly.GetName().Name))
                .Options;

            return new SimplDbContext(options);
        }
    }
}
