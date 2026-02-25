using System.Data;
using Everlore.Domain.Reporting;
using Everlore.Gateway.Configuration;
using Everlore.QueryEngine.Connections;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Npgsql;

namespace Everlore.Gateway.Connections;

public class GatewayConnectionFactory(IOptions<GatewaySettings> settings) : IExternalConnectionFactory
{
    private readonly Dictionary<Guid, DataSourceLocalConfig> _dataSourceMap =
        settings.Value.DataSources.Values.ToDictionary(ds => ds.DataSourceId);

    public async Task<IDbConnection> CreateConnectionAsync(DataSource dataSource, CancellationToken ct = default)
    {
        if (!_dataSourceMap.TryGetValue(dataSource.Id, out var localConfig))
            throw new InvalidOperationException($"Data source '{dataSource.Id}' is not configured in the gateway agent.");

        IDbConnection connection = (DataSourceType)localConfig.DataSourceType switch
        {
            DataSourceType.PostgreSql => new NpgsqlConnection(localConfig.ConnectionString),
            DataSourceType.SqlServer => new SqlConnection(localConfig.ConnectionString),
            DataSourceType.MySql => new MySqlConnection(localConfig.ConnectionString),
            _ => throw new NotSupportedException($"Unsupported data source type: {localConfig.DataSourceType}")
        };

        await ((System.Data.Common.DbConnection)connection).OpenAsync(ct);
        return connection;
    }
}
