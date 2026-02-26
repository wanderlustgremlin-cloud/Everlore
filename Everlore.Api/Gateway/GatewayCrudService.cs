using Everlore.Gateway.Contracts.Messages;
using Everlore.Api.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace Everlore.Api.Gateway;

public class GatewayCrudService(
    IGatewayConnectionTracker connectionTracker,
    IGatewayResponseCorrelator responseCorrelator,
    IHubContext<GatewayHub, Everlore.Gateway.Contracts.IGatewayHubClient> hubContext,
    ILogger<GatewayCrudService> logger)
{
    private static readonly TimeSpan GatewayTimeout = TimeSpan.FromSeconds(60);

    public async Task<GatewayCrudResponse> SendAsync(Guid tenantId, GatewayCrudRequest request, CancellationToken ct = default)
    {
        if (!connectionTracker.IsAgentOnline(tenantId))
            throw new InvalidOperationException("Gateway agent is not connected for this tenant. Cannot execute CRUD operation on self-hosted tenant.");

        responseCorrelator.RegisterRequestTenant(request.RequestId, tenantId);

        logger.LogInformation("Routing CRUD {Operation} {EntityType} {RequestId} to gateway agent for tenant {TenantId}",
            request.Operation, request.EntityType, request.RequestId, tenantId);

        await hubContext.Clients.Group($"gateway:{tenantId}").ExecuteCrud(request);

        return await responseCorrelator.WaitForCrudResponseAsync(request.RequestId, GatewayTimeout, ct);
    }
}
