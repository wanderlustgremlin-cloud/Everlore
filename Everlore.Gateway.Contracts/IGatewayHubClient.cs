using Everlore.Gateway.Contracts.Messages;

namespace Everlore.Gateway.Contracts;

/// <summary>
/// Methods the server calls on the gateway agent.
/// </summary>
public interface IGatewayHubClient
{
    Task ExecuteQuery(GatewayExecuteQueryRequest request);
    Task DiscoverSchema(GatewayDiscoverSchemaRequest request);
    Task Explore(GatewayExploreRequest request);
    Task ExecuteCrud(GatewayCrudRequest request);
    Task Ping();
}
