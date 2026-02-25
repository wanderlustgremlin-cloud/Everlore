using System.Text.Json;
using Everlore.Domain.Reporting;
using Everlore.Gateway.Contracts.Messages;
using Everlore.QueryEngine.Execution;
using Everlore.QueryEngine.Query;
using Microsoft.Extensions.Logging;

namespace Everlore.Gateway.Handlers;

public class ExecuteQueryHandler(
    IQueryExecutionService queryExecutionService,
    ILogger<ExecuteQueryHandler> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<GatewayExecuteQueryResponse> HandleAsync(GatewayExecuteQueryRequest request, CancellationToken ct)
    {
        try
        {
            logger.LogInformation("Executing query {RequestId} for data source {DataSourceId}",
                request.RequestId, request.DataSourceId);

            var queryDefinition = JsonSerializer.Deserialize<QueryDefinition>(request.QueryDefinitionJson, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize query definition.");

            // Build a minimal DataSource for local execution
            var dataSource = new DataSource
            {
                Id = request.DataSourceId,
                Type = (DataSourceType)request.DataSourceType
            };

            var result = await queryExecutionService.ExecuteAsync(queryDefinition, dataSource, ct);

            var resultJson = JsonSerializer.Serialize(result, JsonOptions);

            logger.LogInformation("Query {RequestId} completed successfully with {RowCount} rows",
                request.RequestId, result.RowCount);

            return new GatewayExecuteQueryResponse(request.RequestId, true, resultJson, null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Query {RequestId} failed", request.RequestId);
            return new GatewayExecuteQueryResponse(request.RequestId, false, null, ex.Message);
        }
    }
}
