namespace Everlore.Gateway.Contracts.Messages;

public record GatewayExecuteQueryRequest(
    string RequestId,
    Guid DataSourceId,
    int DataSourceType,
    string QueryDefinitionJson);
