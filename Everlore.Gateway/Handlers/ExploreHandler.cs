using System.Text.Json;
using Dapper;
using Everlore.Domain.Reporting;
using Everlore.Gateway.Contracts.Messages;
using Everlore.QueryEngine.Connections;
using Microsoft.Extensions.Logging;

namespace Everlore.Gateway.Handlers;

public class ExploreHandler(
    IExternalConnectionFactory connectionFactory,
    ILogger<ExploreHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<GatewayExploreResponse> HandleAsync(GatewayExploreRequest request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Executing explore {RequestId} for data source {DataSourceId}",
                request.RequestId, request.DataSourceId);

            var dataSource = new DataSource
            {
                Id = request.DataSourceId,
                Type = (DataSourceType)request.DataSourceType
            };

            using var connection = await connectionFactory.CreateConnectionAsync(dataSource, ct);
            var rows = (await connection.QueryAsync(request.Sql)).ToList();

            var results = rows.Select(r =>
            {
                var dict = (IDictionary<string, object?>)r;
                return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
            }).ToList();

            var resultJson = JsonSerializer.Serialize(results, JsonOptions);

            logger.LogInformation("Explore {RequestId} completed: {RowCount} rows",
                request.RequestId, results.Count);

            return new GatewayExploreResponse(request.RequestId, true, resultJson, results.Count, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Explore {RequestId} failed", request.RequestId);
            return new GatewayExploreResponse(request.RequestId, false, null, 0, ex.Message);
        }
    }
}
