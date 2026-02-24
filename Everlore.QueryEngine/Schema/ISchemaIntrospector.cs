using System.Data;

namespace Everlore.QueryEngine.Schema;

public interface ISchemaIntrospector
{
    Task<DiscoveredSchema> IntrospectAsync(IDbConnection connection, Guid dataSourceId, CancellationToken ct = default);
}
