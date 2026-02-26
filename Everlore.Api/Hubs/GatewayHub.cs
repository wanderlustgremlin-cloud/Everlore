using Everlore.Api.Gateway;
using Everlore.Gateway.Contracts;
using Everlore.Gateway.Contracts.Messages;
using Microsoft.AspNetCore.SignalR;

namespace Everlore.Api.Hubs;

public class GatewayHub(
    IGatewayApiKeyValidator apiKeyValidator,
    IGatewayConnectionTracker connectionTracker,
    IGatewayResponseCorrelator responseCorrelator,
    ILogger<GatewayHub> logger) : Hub<IGatewayHubClient>, IGatewayHubServer
{
    private static readonly string TenantIdKey = "TenantId";

    public async Task<bool> Authenticate(string apiKey)
    {
        var result = await apiKeyValidator.ValidateAsync(apiKey, Context.ConnectionAborted);

        if (!result.IsValid || !result.TenantId.HasValue)
        {
            logger.LogWarning("Gateway authentication failed for connection {ConnectionId}: {Error}",
                Context.ConnectionId, result.Error);
            return false;
        }

        var tenantId = result.TenantId.Value;
        Context.Items[TenantIdKey] = tenantId;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"gateway:{tenantId}");
        connectionTracker.RegisterAgent(Context.ConnectionId, new GatewayAgentInfo(
            tenantId, Context.ConnectionId, null, DateTime.UtcNow, DateTime.UtcNow, []));

        logger.LogInformation("Gateway agent authenticated for tenant {TenantId} on connection {ConnectionId}",
            tenantId, Context.ConnectionId);

        return true;
    }

    public Task SendQueryResult(GatewayExecuteQueryResponse response)
    {
        logger.LogDebug("Received query result for request {RequestId}, success={Success}",
            response.RequestId, response.Success);
        responseCorrelator.CompleteQueryResponse(response.RequestId, response);
        return Task.CompletedTask;
    }

    public Task SendSchemaResult(GatewayDiscoverSchemaResponse response)
    {
        logger.LogDebug("Received schema result for request {RequestId}, success={Success}",
            response.RequestId, response.Success);
        responseCorrelator.CompleteSchemaResponse(response.RequestId, response);
        return Task.CompletedTask;
    }

    public Task SendExploreResult(GatewayExploreResponse response)
    {
        logger.LogDebug("Received explore result for request {RequestId}, success={Success}",
            response.RequestId, response.Success);
        responseCorrelator.CompleteExploreResponse(response.RequestId, response);
        return Task.CompletedTask;
    }

    public Task SendCrudResult(GatewayCrudResponse response)
    {
        logger.LogDebug("Received CRUD result for request {RequestId}, success={Success}",
            response.RequestId, response.Success);
        responseCorrelator.CompleteCrudResponse(response.RequestId, response);
        return Task.CompletedTask;
    }

    public Task SendError(GatewayErrorResponse error)
    {
        logger.LogWarning("Gateway agent sent error for request {RequestId}: {Error}",
            error.RequestId, error.Error);
        responseCorrelator.FailRequest(error.RequestId, error.Error);
        return Task.CompletedTask;
    }

    public Task Heartbeat(GatewayHeartbeat heartbeat)
    {
        logger.LogTrace("Heartbeat from connection {ConnectionId}: TenantId={TenantId}, AgentVersion={AgentVersion}, DataSourceCount={DataSourceCount}",
            Context.ConnectionId,
            Context.Items.TryGetValue(TenantIdKey, out var tid) ? tid : "unknown",
            heartbeat.AgentVersion,
            heartbeat.AvailableDataSourceIds.Count);

        connectionTracker.UpdateHeartbeat(
            Context.ConnectionId, heartbeat.AgentVersion, heartbeat.AvailableDataSourceIds);
        return Task.CompletedTask;
    }

    public override Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue(TenantIdKey, out var tenantIdObj) && tenantIdObj is Guid tenantId)
        {
            var agentInfo = connectionTracker.GetAgentInfo(tenantId);
            var duration = agentInfo is not null ? DateTime.UtcNow - agentInfo.ConnectedAt : TimeSpan.Zero;

            logger.LogInformation(
                "Gateway agent disconnected for tenant {TenantId} (connection {ConnectionId}, duration={DurationMinutes:F1}min){ExceptionMessage}",
                tenantId, Context.ConnectionId, duration.TotalMinutes,
                exception is not null ? $": {exception.Message}" : "");

            connectionTracker.UnregisterAgent(Context.ConnectionId);
            responseCorrelator.CancelPendingRequests(tenantId);
        }

        return base.OnDisconnectedAsync(exception);
    }
}
