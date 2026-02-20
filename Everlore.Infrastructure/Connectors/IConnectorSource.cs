namespace Everlore.Infrastructure.Connectors;

public interface IConnectorSource
{
    string ConnectorType { get; }
    Task InitializeAsync(string? settings, CancellationToken ct = default);
}
