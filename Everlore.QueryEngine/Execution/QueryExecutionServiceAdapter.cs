using System.Text.Json;
using Everlore.Application.Common.Interfaces;
using Everlore.Application.Reporting.Queries;
using Everlore.Domain.Reporting;
using Everlore.QueryEngine.Query;

namespace Everlore.QueryEngine.Execution;

public class QueryExecutionServiceAdapter(IQueryExecutionService inner) : Application.Common.Interfaces.IQueryExecutionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<object> ExecuteAsync(ExecuteQueryCommand command, DataSource dataSource, CancellationToken ct = default)
    {
        var query = MapToQueryDefinition(command);
        return await inner.ExecuteAsync(query, dataSource, ct);
    }

    public async Task<object> ExecuteReportAsync(ReportDefinition report, DataSource dataSource, CancellationToken ct = default)
    {
        var query = JsonSerializer.Deserialize<QueryDefinition>(report.QueryDefinitionJson, JsonOptions)
            ?? throw new InvalidOperationException("Failed to deserialize report query definition.");
        query.DataSourceId = report.DataSourceId;
        return await inner.ExecuteAsync(query, dataSource, ct);
    }

    private static QueryDefinition MapToQueryDefinition(ExecuteQueryCommand command)
    {
        var query = new QueryDefinition
        {
            DataSourceId = command.DataSourceId,
            Table = command.Table,
            SchemaName = command.SchemaName,
            Limit = command.Limit,
            Offset = command.Offset
        };

        if (command.Measures is JsonElement measuresJson)
            query.Measures = JsonSerializer.Deserialize<List<Measure>>(measuresJson.GetRawText(), JsonOptions) ?? [];

        if (command.Dimensions is JsonElement dimensionsJson)
            query.Dimensions = JsonSerializer.Deserialize<List<Dimension>>(dimensionsJson.GetRawText(), JsonOptions) ?? [];

        if (command.Filters is JsonElement filtersJson)
            query.Filters = JsonSerializer.Deserialize<List<QueryFilter>>(filtersJson.GetRawText(), JsonOptions) ?? [];

        if (command.Sorts is JsonElement sortsJson)
            query.Sorts = JsonSerializer.Deserialize<List<QuerySort>>(sortsJson.GetRawText(), JsonOptions) ?? [];

        return query;
    }
}
