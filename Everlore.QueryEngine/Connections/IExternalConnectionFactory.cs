using System.Data;
using Everlore.Domain.Reporting;

namespace Everlore.QueryEngine.Connections;

public interface IExternalConnectionFactory
{
    Task<IDbConnection> CreateConnectionAsync(DataSource dataSource, CancellationToken ct = default);
}
