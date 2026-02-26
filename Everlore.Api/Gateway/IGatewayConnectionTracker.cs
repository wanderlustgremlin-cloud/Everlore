namespace Everlore.Api.Gateway;

public record GatewayAgentInfo(
    Guid TenantId,
    string ConnectionId,
    string? AgentVersion,
    DateTime ConnectedAt,
    DateTime LastHeartbeatAt,
    IReadOnlyList<Guid> AvailableDataSourceIds);

public interface IGatewayConnectionTracker
{
    void RegisterAgent(string connectionId, GatewayAgentInfo info);
    void UnregisterAgent(string connectionId);
    void UpdateHeartbeat(string connectionId, string agentVersion, IReadOnlyList<Guid> dataSourceIds);
    bool IsAgentOnline(Guid tenantId);
    bool IsAgentHealthy(Guid tenantId);
    GatewayAgentInfo? GetAgentInfo(Guid tenantId);
    string? GetConnectionId(Guid tenantId);
}
