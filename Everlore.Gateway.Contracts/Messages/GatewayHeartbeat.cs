namespace Everlore.Gateway.Contracts.Messages;

public record GatewayHeartbeat(
    string AgentVersion,
    IReadOnlyList<Guid> AvailableDataSourceIds,
    DateTime Timestamp);
