using Everlore.Infrastructure.Connectors;
using Everlore.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Everlore.SyncService;

public class SyncWorker(
    IServiceProvider serviceProvider,
    IEnumerable<IConnectorSource> connectorSources,
    IEnumerable<ISyncHandler> syncHandlers,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<SyncWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var sourcesByType = connectorSources.ToDictionary(s => s.ConnectorType);
        var handlersByType = syncHandlers.ToDictionary(h => h.ConnectorType);

        using var scope = serviceProvider.CreateScope();
        var catalogDb = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var configs = await catalogDb.ConnectorConfigurations
            .Include(c => c.Tenant)
            .Where(c => c.IsEnabled && c.Tenant.IsActive)
            .ToListAsync(stoppingToken);

        logger.LogInformation("Found {Count} active connector configuration(s)", configs.Count);

        foreach (var config in configs)
        {
            try
            {
                if (!sourcesByType.TryGetValue(config.ConnectorType, out var source))
                {
                    logger.LogWarning("No connector source registered for type '{Type}', skipping tenant '{Tenant}'",
                        config.ConnectorType, config.Tenant.Identifier);
                    continue;
                }

                if (!handlersByType.TryGetValue(config.ConnectorType, out var handler))
                {
                    logger.LogWarning("No sync handler registered for type '{Type}', skipping tenant '{Tenant}'",
                        config.ConnectorType, config.Tenant.Identifier);
                    continue;
                }

                logger.LogInformation("Syncing connector '{Type}' for tenant '{Tenant}'",
                    config.ConnectorType, config.Tenant.Identifier);

                await source.InitializeAsync(config.Settings, stoppingToken);

                var tenantOptions = new DbContextOptionsBuilder<EverloreDbContext>()
                    .UseNpgsql(config.Tenant.ConnectionString, o =>
                        o.MigrationsAssembly("Everlore.Infrastructure.Postgres"))
                    .Options;

                await using var tenantDb = new EverloreDbContext(tenantOptions);
                await handler.SyncAsync(source, tenantDb, stoppingToken);

                logger.LogInformation("Completed sync for tenant '{Tenant}'", config.Tenant.Identifier);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to sync connector '{Type}' for tenant '{Tenant}'",
                    config.ConnectorType, config.Tenant.Identifier);
            }
        }

        hostApplicationLifetime.StopApplication();
    }
}
