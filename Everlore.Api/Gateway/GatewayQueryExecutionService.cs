using System.Text.Json;
using Everlore.Application.Common.Interfaces;
using Everlore.Application.Reporting.Queries;
using Everlore.Domain.Reporting;
using Everlore.Domain.Tenancy;
using Everlore.Gateway.Contracts.Messages;
using Everlore.Api.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Everlore.Api.Gateway;

public class GatewayQueryExecutionService(
    IQueryExecutionService inner,
    ICatalogDbContext db,
    IGatewayConnectionTracker connectionTracker,
    IGatewayResponseCorrelator responseCorrelator,
    IHubContext<GatewayHub, Everlore.Gateway.Contracts.IGatewayHubClient> hubContext,
    ILogger<GatewayQueryExecutionService> logger) : IQueryExecutionService
{
    private static readonly TimeSpan GatewayTimeout = TimeSpan.FromSeconds(60);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public async Task<object> ExecuteAsync(ExecuteQueryCommand query, DataSource dataSource, CancellationToken ct = default)
    {
        if (!await IsSelfHostedAsync(dataSource.TenantId, ct))
            return await inner.ExecuteAsync(query, dataSource, ct);

        return await RouteQueryThroughGateway(query, dataSource, ct);
    }

    public async Task<object> ExecuteReportAsync(ReportDefinition report, DataSource dataSource, CancellationToken ct = default)
    {
        if (!await IsSelfHostedAsync(dataSource.TenantId, ct))
            return await inner.ExecuteReportAsync(report, dataSource, ct);

        // For reports, deserialize the stored query and route through gateway
        var command = new ExecuteQueryCommand(
            report.DataSourceId, string.Empty, null, null, null, null, null, null, null);

        return await RouteQueryThroughGateway(command, dataSource, ct, report.QueryDefinitionJson);
    }

    private async Task<object> RouteQueryThroughGateway(
        ExecuteQueryCommand query, DataSource dataSource, CancellationToken ct, string? overrideQueryJson = null)
    {
        var tenantId = dataSource.TenantId;

        if (!connectionTracker.IsAgentOnline(tenantId))
            throw new InvalidOperationException("Gateway agent is not connected for this tenant. Cannot execute query on self-hosted data source.");

        var requestId = Guid.NewGuid().ToString("N");
        var queryJson = overrideQueryJson ?? JsonSerializer.Serialize(query, JsonOptions);

        var request = new GatewayExecuteQueryRequest(
            requestId, dataSource.Id, (int)dataSource.Type, queryJson);

        responseCorrelator.RegisterRequestTenant(requestId, tenantId);

        logger.LogInformation("Routing query {RequestId} to gateway agent for tenant {TenantId}, data source {DataSourceId}",
            requestId, tenantId, dataSource.Id);

        await hubContext.Clients.Group($"gateway:{tenantId}").ExecuteQuery(request);

        var response = await responseCorrelator.WaitForQueryResponseAsync(requestId, GatewayTimeout, ct);

        if (!response.Success)
            throw new InvalidOperationException($"Gateway query failed: {response.Error}");

        return JsonSerializer.Deserialize<object>(response.ResultJson!, JsonOptions)!;
    }

    private async Task<bool> IsSelfHostedAsync(Guid tenantId, CancellationToken ct)
    {
        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId, ct);

        return tenant?.HostingMode == HostingMode.SelfHosted;
    }
}
