using Everlore.Application.Common.Interfaces;
using Everlore.QueryEngine.Caching;
using Everlore.QueryEngine.Connections;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ISchemaService = Everlore.Application.Common.Interfaces.ISchemaService;

namespace Everlore.QueryEngine.Schema;

public class SchemaService(
    ICatalogDbContext db,
    IExternalConnectionFactory connectionFactory,
    SchemaIntrospectorFactory introspectorFactory,
    IQueryCacheService cache,
    ILogger<SchemaService> logger) : ISchemaService
{
    public async Task<object> GetSchemaAsync(Guid dataSourceId, bool forceRefresh = false, CancellationToken ct = default)
    {
        var dataSource = await db.DataSources
            .FirstOrDefaultAsync(ds => ds.Id == dataSourceId, ct)
            ?? throw new Application.Common.Exceptions.NotFoundException("DataSource", dataSourceId);

        var cacheKey = DistributedQueryCacheService.SchemaKey(dataSource.TenantId, dataSourceId);

        if (!forceRefresh)
        {
            var cached = await cache.GetAsync<DiscoveredSchema>(cacheKey, ct);
            if (cached is not null)
                return cached;
        }

        logger.LogInformation("Introspecting schema for data source {DataSourceId} ({Type})", dataSourceId, dataSource.Type);

        using var connection = await connectionFactory.CreateConnectionAsync(dataSource, ct);
        var introspector = introspectorFactory.Create(dataSource.Type);
        var schema = await introspector.IntrospectAsync(connection, dataSourceId, ct);

        await cache.SetAsync(cacheKey, schema, TimeSpan.FromHours(1), ct);

        dataSource.SchemaLastRefreshedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        logger.LogInformation("Schema introspection complete for {DataSourceId}: {TableCount} tables",
            dataSourceId, schema.Tables.Count);

        return schema;
    }

    public async Task<DiscoveredSchema> GetDiscoveredSchemaAsync(Guid dataSourceId, bool forceRefresh = false, CancellationToken ct = default)
    {
        return (DiscoveredSchema)await GetSchemaAsync(dataSourceId, forceRefresh, ct);
    }
}
