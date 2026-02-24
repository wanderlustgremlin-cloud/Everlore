using System.Data;
using Everlore.Domain.Reporting;
using Microsoft.Extensions.Logging;
using Polly;

namespace Everlore.QueryEngine.Connections;

public class ResilientConnectionFactory(
    ExternalConnectionFactory inner,
    ResiliencePipeline pipeline,
    ILogger<ResilientConnectionFactory> logger) : IExternalConnectionFactory
{
    public async Task<IDbConnection> CreateConnectionAsync(DataSource dataSource, CancellationToken ct = default)
    {
        return await pipeline.ExecuteAsync(async token =>
        {
            logger.LogDebug("Opening connection to {DataSourceType} data source {DataSourceId}",
                dataSource.Type, dataSource.Id);
            return await inner.CreateConnectionAsync(dataSource, token);
        }, ct);
    }
}
