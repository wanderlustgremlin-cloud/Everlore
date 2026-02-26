namespace Everlore.Gateway.Contracts.Messages;

public record GatewayExploreResponse(string RequestId, bool Success, string? ResultJson, int RowCount, string? Error);
