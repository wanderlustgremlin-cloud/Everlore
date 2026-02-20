using Everlore.Infrastructure.Persistence;

namespace Everlore.Infrastructure.Connectors;

public interface ISyncHandler
{
    string ConnectorType { get; }
    Task SyncAsync(IConnectorSource source, EverloreDbContext targetDb, CancellationToken ct = default);
}
