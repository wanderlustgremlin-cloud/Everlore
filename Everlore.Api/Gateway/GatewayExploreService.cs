using System.Text.Json;
using Everlore.Application.Common.Interfaces;
using Everlore.Domain.Tenancy;
using Everlore.Gateway.Contracts.Messages;
using Everlore.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Api.Gateway;

public class GatewayExploreService(
    IExploreService inner,
    ICatalogDbContext db,
    IGatewayConnectionTracker connectionTracker,
    IGatewayResponseCorrelator responseCorrelator,
    IHubContext<GatewayHub, Everlore.Gateway.Contracts.IGatewayHubClient> hubContext,
    ICurrentUser currentUser,
    ILogger<GatewayExploreService> logger) : IExploreService
{
    private static readonly TimeSpan GatewayTimeout = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<IReadOnlyList<Dictionary<string, object?>>> ExploreAsync(
        Guid dataSourceId, int dataSourceType, string sql, CancellationToken ct = default)
    {
        var tenantId = currentUser.TenantId;
        if (tenantId is null || !await IsSelfHostedAsync(tenantId.Value, ct))
            return await inner.ExploreAsync(dataSourceId, dataSourceType, sql, ct);

        return await RouteExploreThroughGateway(tenantId.Value, dataSourceId, dataSourceType, sql, ct);
    }

    private async Task<IReadOnlyList<Dictionary<string, object?>>> RouteExploreThroughGateway(
        Guid tenantId, Guid dataSourceId, int dataSourceType, string sql, CancellationToken ct)
    {
        if (!connectionTracker.IsAgentOnline(tenantId))
            throw new InvalidOperationException("Gateway agent is not connected for this tenant. Cannot execute explore query on self-hosted data source.");

        var requestId = Guid.NewGuid().ToString("N");
        var request = new GatewayExploreRequest(requestId, dataSourceId, dataSourceType, sql);

        responseCorrelator.RegisterRequestTenant(requestId, tenantId);

        logger.LogInformation("Routing explore {RequestId} to gateway agent for tenant {TenantId}, data source {DataSourceId}",
            requestId, tenantId, dataSourceId);

        await hubContext.Clients.Group($"gateway:{tenantId}").Explore(request);

        var response = await responseCorrelator.WaitForExploreResponseAsync(requestId, GatewayTimeout, ct);

        if (!response.Success)
            throw new InvalidOperationException($"Gateway explore failed: {response.Error}");

        return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(response.ResultJson!, JsonOptions) ?? [];
    }

    private async Task<bool> IsSelfHostedAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        return tenant?.HostingMode == HostingMode.SelfHosted;
    }
}
