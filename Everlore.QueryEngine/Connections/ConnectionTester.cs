using Everlore.Application.Common.Interfaces;
using Everlore.Application.Reporting.DataSources;
using Everlore.Domain.Reporting;

namespace Everlore.QueryEngine.Connections;

public class ConnectionTester(IExternalConnectionFactory connectionFactory) : IConnectionTester
{
    public async Task<TestConnectionResult> TestAsync(DataSource dataSource, CancellationToken ct)
    {
        try
        {
            using var connection = await connectionFactory.CreateConnectionAsync(dataSource, ct);
            return new TestConnectionResult(true, "Connection successful.");
        }
        catch (Exception ex)
        {
            return new TestConnectionResult(false, $"Connection failed: {ex.Message}");
        }
    }
}
