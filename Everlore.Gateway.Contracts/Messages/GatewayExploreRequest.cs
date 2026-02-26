namespace Everlore.Gateway.Contracts.Messages;

public record GatewayExploreRequest(string RequestId, Guid DataSourceId, int DataSourceType, string Sql);
