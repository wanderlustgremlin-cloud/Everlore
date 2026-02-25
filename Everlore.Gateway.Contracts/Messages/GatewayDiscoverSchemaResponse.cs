namespace Everlore.Gateway.Contracts.Messages;

public record GatewayDiscoverSchemaResponse(
    string RequestId,
    bool Success,
    string? SchemaJson,
    string? Error);
