using Everlore.Application.Common.Interfaces;
using Everlore.QueryEngine.Connections;
using Everlore.QueryEngine.Schema;
using Microsoft.Extensions.Logging;

namespace Everlore.Gateway.Connections;

/// <summary>
/// Local schema service for the gateway agent. Introspects schemas directly
/// via the local connection factory (no catalog DB required).
/// </summary>
public class GatewaySchemaService(
    IExternalConnectionFactory connectionFactory,
    SchemaIntrospectorFactory introspectorFactory,
    ILogger<GatewaySchemaService> logger) : ISchemaService
{
    public async Task<object> GetSchemaAsync(Guid dataSourceId, bool forceRefresh = false, CancellationToken ct = default)
    {
        logger.LogInformation("Introspecting schema locally for data source {DataSourceId}", dataSourceId);

        // Build a minimal DataSource to open a connection via the local factory
        var dataSource = new Domain.Reporting.DataSource { Id = dataSourceId };

        using var connection = await connectionFactory.CreateConnectionAsync(dataSource, ct);

        // Determine dialect from the connection type
        var dataSourceType = connection switch
        {
            Npgsql.NpgsqlConnection => Domain.Reporting.DataSourceType.PostgreSql,
            Microsoft.Data.SqlClient.SqlConnection => Domain.Reporting.DataSourceType.SqlServer,
            MySqlConnector.MySqlConnection => Domain.Reporting.DataSourceType.MySql,
            _ => throw new NotSupportedException($"Unsupported connection type: {connection.GetType().Name}")
        };

        var introspector = introspectorFactory.Create(dataSourceType);
        var schema = await introspector.IntrospectAsync(connection, dataSourceId, ct);

        logger.LogInformation("Schema introspection complete for {DataSourceId}: {TableCount} tables",
            dataSourceId, schema.Tables.Count);

        return schema;
    }
}
