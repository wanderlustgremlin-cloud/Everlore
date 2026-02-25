using System.Data;
using System.Text.Json;
using Everlore.Domain.Reporting;
using Everlore.Gateway.Contracts.Messages;
using Everlore.QueryEngine.Connections;
using Everlore.QueryEngine.Schema;
using Microsoft.Extensions.Logging;

namespace Everlore.Gateway.Handlers;

public class DiscoverSchemaHandler(
    IExternalConnectionFactory connectionFactory,
    SchemaIntrospectorFactory introspectorFactory,
    ILogger<DiscoverSchemaHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<GatewayDiscoverSchemaResponse> HandleAsync(GatewayDiscoverSchemaRequest request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Discovering schema {RequestId} for data source {DataSourceId}",
                request.RequestId, request.DataSourceId);

            var dataSourceType = (DataSourceType)request.DataSourceType;

            // Build minimal DataSource for connection
            var dataSource = new DataSource
            {
                Id = request.DataSourceId,
                Type = dataSourceType
            };

            using var connection = await connectionFactory.CreateConnectionAsync(dataSource, ct);
            var introspector = introspectorFactory.Create(dataSourceType);
            var schema = await introspector.IntrospectAsync(connection, request.DataSourceId, ct);

            var schemaJson = JsonSerializer.Serialize(schema, JsonOptions);

            logger.LogInformation("Schema discovery {RequestId} completed: {TableCount} tables",
                request.RequestId, schema.Tables.Count);

            return new GatewayDiscoverSchemaResponse(request.RequestId, true, schemaJson, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Schema discovery {RequestId} failed", request.RequestId);
            return new GatewayDiscoverSchemaResponse(request.RequestId, false, null, ex.Message);
        }
    }
}
