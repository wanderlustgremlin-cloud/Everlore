namespace Everlore.Gateway.Contracts.Messages;

public record GatewayErrorResponse(
    string RequestId,
    string Error);
