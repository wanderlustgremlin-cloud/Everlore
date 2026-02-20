using Everlore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Everlore.Infrastructure.Postgres;

public class DesignTimeEverloreDbContextFactory : IDesignTimeDbContextFactory<EverloreDbContext>
{
    public EverloreDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<EverloreDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=everlore_tenant;Username=postgres;Password=postgres", o =>
            o.MigrationsAssembly("Everlore.Infrastructure.Postgres"));

        return new EverloreDbContext(optionsBuilder.Options);
    }
}
