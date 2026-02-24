using System.Data;
using Everlore.Application.Common.Interfaces;
using Everlore.Domain.Reporting;
using Microsoft.Data.SqlClient;
using MySqlConnector;
using Npgsql;

namespace Everlore.QueryEngine.Connections;

public class ExternalConnectionFactory(IEncryptionService encryption) : IExternalConnectionFactory
{
    public async Task<IDbConnection> CreateConnectionAsync(DataSource dataSource, CancellationToken ct = default)
    {
        var connectionString = encryption.Decrypt(dataSource.EncryptedConnectionString);

        IDbConnection connection = dataSource.Type switch
        {
            DataSourceType.PostgreSql => new NpgsqlConnection(connectionString),
            DataSourceType.SqlServer => new SqlConnection(connectionString),
            DataSourceType.MySql => new MySqlConnection(connectionString),
            _ => throw new NotSupportedException($"Unsupported data source type: {dataSource.Type}")
        };

        await ((System.Data.Common.DbConnection)connection).OpenAsync(ct);
        return connection;
    }
}
