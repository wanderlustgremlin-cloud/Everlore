using System.Text.Json;
using Everlore.Application.Common.Interfaces;
using Everlore.Domain.Tenancy;
using Everlore.Gateway.Contracts.Messages;
using Everlore.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Api.Gateway;

public class GatewaySchemaService(
    ISchemaService inner,
    ICatalogDbContext db,
    IGatewayConnectionTracker connectionTracker,
    IGatewayResponseCorrelator responseCorrelator,
    IHubContext<GatewayHub, Everlore.Gateway.Contracts.IGatewayHubClient> hubContext,
    ILogger<GatewaySchemaService> logger) : ISchemaService
{
    private static readonly TimeSpan GatewayTimeout = TimeSpan.FromSeconds(60);

    public async Task<object> GetSchemaAsync(Guid dataSourceId, bool forceRefresh = false, CancellationToken ct = default)
    {
        var dataSource = await db.DataSources
            .AsNoTracking()
            .FirstOrDefaultAsync(ds => ds.Id == dataSourceId, ct);

        if (dataSource is null)
            return await inner.GetSchemaAsync(dataSourceId, forceRefresh, ct);

        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == dataSource.TenantId, ct);

        if (tenant?.HostingMode != HostingMode.SelfHosted)
            return await inner.GetSchemaAsync(dataSourceId, forceRefresh, ct);

        // Route through gateway
        var tenantId = dataSource.TenantId;

        if (!connectionTracker.IsAgentOnline(tenantId))
            throw new InvalidOperationException("Gateway agent is not connected for this tenant. Cannot discover schema on self-hosted data source.");

        var requestId = Guid.NewGuid().ToString("N");
        var request = new GatewayDiscoverSchemaRequest(
            requestId, dataSourceId, (int)dataSource.Type, forceRefresh);

        responseCorrelator.RegisterRequestTenant(requestId, tenantId);

        logger.LogInformation("Routing schema discovery {RequestId} to gateway agent for tenant {TenantId}, data source {DataSourceId}",
            requestId, tenantId, dataSourceId);

        await hubContext.Clients.Group($"gateway:{tenantId}").DiscoverSchema(request);

        var response = await responseCorrelator.WaitForSchemaResponseAsync(requestId, GatewayTimeout, ct);

        if (!response.Success)
            throw new InvalidOperationException($"Gateway schema discovery failed: {response.Error}");

        return JsonSerializer.Deserialize<object>(response.SchemaJson!)!;
    }
}
