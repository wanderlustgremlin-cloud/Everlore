using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Everlore.Domain.Reporting;
using Everlore.QueryEngine.Caching;
using Everlore.QueryEngine.Connections;
using Everlore.QueryEngine.Query;
using Everlore.QueryEngine.Schema;
using Everlore.QueryEngine.Translation;
using Microsoft.Extensions.Logging;
using ISchemaService = Everlore.Application.Common.Interfaces.ISchemaService;

namespace Everlore.QueryEngine.Execution;

public class QueryExecutionService(
    IExternalConnectionFactory connectionFactory,
    SqlTranslatorFactory translatorFactory,
    ISchemaService schemaService,
    IQueryCacheService cache,
    IQueryProgressNotifier? progressNotifier,
    ILogger<QueryExecutionService> logger) : IQueryExecutionService
{
    private const int DefaultMaxRowLimit = 10_000;
    private const int QueryTimeoutSeconds = 60;

    public async Task<QueryResult> ExecuteAsync(QueryDefinition query, DataSource dataSource, CancellationToken ct = default)
    {
        var operationId = Guid.NewGuid().ToString("N");

        try
        {
            await NotifyProgress(dataSource.TenantId, operationId, "Validating", 10, ct: ct);

            // Get schema for column validation
            var schemaObj = await schemaService.GetSchemaAsync(dataSource.Id, forceRefresh: false, ct);
            var schema = (DiscoveredSchema)schemaObj;

            var validColumns = schema.Tables
                .Where(t => t.TableName.Equals(query.Table, StringComparison.OrdinalIgnoreCase)
                    && (query.SchemaName is null || t.SchemaName.Equals(query.SchemaName, StringComparison.OrdinalIgnoreCase)))
                .SelectMany(t => t.Columns)
                .Select(c => c.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            if (validColumns.Count == 0)
                throw new InvalidOperationException($"Table '{query.Table}' not found in data source schema.");

            // Enforce row limit
            var maxRows = query.Limit.HasValue ? Math.Min(query.Limit.Value, DefaultMaxRowLimit) : DefaultMaxRowLimit;
            query.Limit = maxRows;

            // Translate to SQL
            await NotifyProgress(dataSource.TenantId, operationId, "Translating", 25, ct: ct);
            var translator = translatorFactory.Create(dataSource.Type);
            var (sql, parameters) = translator.Translate(query, validColumns);

            logger.LogInformation("Executing query on {DataSourceId}: {Sql}", dataSource.Id, sql);

            // Check cache
            var queryHash = ComputeHash(sql, parameters);
            var cacheKey = DistributedQueryCacheService.QueryKey(dataSource.TenantId, dataSource.Id, queryHash);
            var cached = await cache.GetAsync<QueryResult>(cacheKey, ct);
            if (cached is not null)
            {
                logger.LogDebug("Query result served from cache");
                await NotifyCompleted(dataSource.TenantId, operationId, cached.RowCount, cached.ExecutionTime, ct);
                return cached;
            }

            // Execute
            await NotifyProgress(dataSource.TenantId, operationId, "Executing", 50, ct: ct);
            var sw = Stopwatch.StartNew();

            using var connection = await connectionFactory.CreateConnectionAsync(dataSource, ct);

            var commandDefinition = new CommandDefinition(
                sql, parameters,
                commandTimeout: QueryTimeoutSeconds,
                cancellationToken: ct);

            var rows = (await connection.QueryAsync(commandDefinition)).ToList();
            sw.Stop();

            await NotifyProgress(dataSource.TenantId, operationId, "Building result", 90, ct: ct);

            // Build result
            var result = new QueryResult
            {
                ExecutionTime = sw.Elapsed,
                RowCount = rows.Count
            };

            if (rows.Count > 0)
            {
                var firstRow = (IDictionary<string, object?>)rows[0];
                result.Columns = firstRow.Keys.Select(k => new QueryColumn
                {
                    Name = k,
                    Type = firstRow[k]?.GetType().Name ?? "String"
                }).ToList();

                result.Rows = rows.Select(r =>
                {
                    var dict = (IDictionary<string, object?>)r;
                    return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
                }).ToList();
            }

            // Cache result
            await cache.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5), ct);

            await NotifyCompleted(dataSource.TenantId, operationId, result.RowCount, result.ExecutionTime, ct);

            return result;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            await NotifyFailed(dataSource.TenantId, operationId, ex.Message, ct);
            throw;
        }
    }

    private static string ComputeHash(string sql, DynamicParameters parameters)
    {
        var input = sql + JsonSerializer.Serialize(parameters);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private Task NotifyProgress(Guid tenantId, string operationId, string stage, int? percent = null, string? message = null, CancellationToken ct = default)
        => progressNotifier?.NotifyProgressAsync(tenantId, operationId, stage, percent, message, ct) ?? Task.CompletedTask;

    private Task NotifyCompleted(Guid tenantId, string operationId, int rowCount, TimeSpan executionTime, CancellationToken ct)
        => progressNotifier?.NotifyCompletedAsync(tenantId, operationId, rowCount, executionTime, ct) ?? Task.CompletedTask;

    private Task NotifyFailed(Guid tenantId, string operationId, string error, CancellationToken ct)
        => progressNotifier?.NotifyFailedAsync(tenantId, operationId, error, ct) ?? Task.CompletedTask;
}
