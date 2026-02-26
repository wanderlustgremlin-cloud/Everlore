using Everlore.Gateway.Contracts.Messages;

namespace Everlore.Gateway.Contracts;

/// <summary>
/// Methods the gateway agent calls on the server.
/// </summary>
public interface IGatewayHubServer
{
    Task<bool> Authenticate(string apiKey);
    Task SendQueryResult(GatewayExecuteQueryResponse response);
    Task SendSchemaResult(GatewayDiscoverSchemaResponse response);
    Task SendExploreResult(GatewayExploreResponse response);
    Task SendCrudResult(GatewayCrudResponse response);
    Task SendError(GatewayErrorResponse error);
    Task Heartbeat(GatewayHeartbeat heartbeat);
}
