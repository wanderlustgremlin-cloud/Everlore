namespace Everlore.Gateway.Contracts.Messages;

public record GatewayCrudResponse(
    string RequestId,
    bool Success,
    string? ResultJson,
    int StatusCode,
    string? Error);
