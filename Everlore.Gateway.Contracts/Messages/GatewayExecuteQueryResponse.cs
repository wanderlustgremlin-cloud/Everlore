namespace Everlore.Gateway.Contracts.Messages;

public record GatewayExecuteQueryResponse(
    string RequestId,
    bool Success,
    string? ResultJson,
    string? Error);
