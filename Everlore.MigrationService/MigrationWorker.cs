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

        var devTenant = await SeedDevTenantAsync(catalogDb, cancellationToken);
        await SeedConnectorConfigAsync(catalogDb, devTenant, cancellationToken);

        var tenantDb = scope.ServiceProvider.GetRequiredService<EverloreDbContext>();
        await tenantDb.Database.MigrateAsync(cancellationToken);

        hostApplicationLifetime.StopApplication();
    }

    private async Task<Tenant> SeedDevTenantAsync(CatalogDbContext catalogDb, CancellationToken cancellationToken)
    {
        var tenantConnectionString = configuration.GetConnectionString("everloretenantdb")!;

        var devTenant = await catalogDb.Tenants
            .FirstOrDefaultAsync(t => t.Identifier == "dev", cancellationToken);

        if (devTenant is null)
        {
            devTenant = new Tenant
            {
                Name = "Development",
                Identifier = "dev",
                ConnectionString = tenantConnectionString,
                IsActive = true
            };
            catalogDb.Tenants.Add(devTenant);
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
        else if (devTenant.ConnectionString != tenantConnectionString)
        {
            devTenant.ConnectionString = tenantConnectionString;
            await catalogDb.SaveChangesAsync(cancellationToken);
        }

        return devTenant;
    }

    private static async Task SeedConnectorConfigAsync(CatalogDbContext catalogDb, Tenant devTenant, CancellationToken cancellationToken)
    {
        if (!await catalogDb.ConnectorConfigurations.AnyAsync(
                c => c.TenantId == devTenant.Id && c.ConnectorType == "seed", cancellationToken))
        {
            catalogDb.ConnectorConfigurations.Add(new ConnectorConfiguration
            {
                TenantId = devTenant.Id,
                ConnectorType = "seed",
                IsEnabled = true,
                Settings = null
            });
            await catalogDb.SaveChangesAsync(cancellationToken);
        }
    }
}
