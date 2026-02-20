using Everlore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Everlore.Infrastructure.Postgres;

public class DesignTimeCatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=everlore_catalog;Username=postgres;Password=postgres", o =>
            o.MigrationsAssembly("Everlore.Infrastructure.Postgres"));

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
