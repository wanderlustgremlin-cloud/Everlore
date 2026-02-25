namespace Everlore.Gateway.Contracts.Messages;

public record GatewayDiscoverSchemaRequest(
    string RequestId,
    Guid DataSourceId,
    int DataSourceType,
    bool ForceRefresh);
