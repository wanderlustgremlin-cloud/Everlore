using Everlore.Domain.Tenancy;
using Everlore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Everlore.MigrationService;

public class MigrationWorker(
    IServiceProvider serviceProvider,
    IConfiguration configuration,
    IHostApplicationLifetime hostApplicationLifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        using var scope = serviceProvider.CreateScope();

        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        await catalogDb.Database.MigrateAsync(cancellationToken);

        await SeedDevTenantAsync(catalogDb, cancellationToken);

        var tenantDb = scope.ServiceProvider.GetRequiredService<EverloreDbContext>();
        await tenantDb.Database.MigrateAsync(cancellationToken);

        hostApplicationLifetime.StopApplication();
    }

    private async Task SeedDevTenantAsync(CatalogDbContext catalogDb, CancellationToken cancellationToken)
    {
        var tenantConnectionString = configuration.GetConnectionString("everloretenantdb")!;

        var devTenant = await catalogDb.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == "dev", cancellationToken);

        if (devTenant is null)
        {
            catalogDb.Tenants.Add(new Tenant
            {
                Name = "Development",
                Identifier = "dev",
                ConnectionString = tenantConnectionString,
                IsActive = true
            });
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
        else if (devTenant.ConnectionString != tenantConnectionString)
        {
            devTenant.ConnectionString = tenantConnectionString;
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
    }
}
