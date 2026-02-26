using System.Data;
using Dapper;
using Everlore.Application.Common.Interfaces;
using Everlore.Domain.Reporting;
using Everlore.QueryEngine.Connections;

namespace Everlore.QueryEngine.GraphQL;

public class LocalExploreService(IExternalConnectionFactory connectionFactory) : IExploreService
{
    public async Task<IReadOnlyList<Dictionary<string, object?>>> ExploreAsync(
        Guid dataSourceId, int dataSourceType, string sql, CancellationToken ct = default)
    {
        var dataSource = new DataSource
        {
            Id = dataSourceId,
            Type = (DataSourceType)dataSourceType
        };

        using var connection = await connectionFactory.CreateConnectionAsync(dataSource, ct);
        var rows = (await connection.QueryAsync(sql)).ToList();

        return rows.Select(r =>
        {
            var dict = (IDictionary<string, object?>)r;
            return dict.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }).ToList();
    }
}
