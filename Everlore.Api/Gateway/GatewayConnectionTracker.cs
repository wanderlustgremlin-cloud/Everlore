using System.Collections.Concurrent;

namespace Everlore.Api.Gateway;

public class GatewayConnectionTracker : IGatewayConnectionTracker
{
    private readonly ConcurrentDictionary<string, GatewayAgentInfo> _connections = new();
    private readonly ConcurrentDictionary<Guid, string> _tenantToConnection = new();

    public void RegisterAgent(string connectionId, GatewayAgentInfo info)
    {
        _connections[connectionId] = info;
        _tenantToConnection[info.TenantId] = connectionId;
    }

    public void UnregisterAgent(string connectionId)
    {
        if (_connections.TryRemove(connectionId, out var info))
        {
            _tenantToConnection.TryRemove(info.TenantId, out _);
        }
    }

    public void UpdateHeartbeat(string connectionId, string agentVersion, IReadOnlyList<Guid> dataSourceIds)
    {
        if (_connections.TryGetValue(connectionId, out var existing))
        {
            var updated = existing with
            {
                AgentVersion = agentVersion,
                LastHeartbeatAt = DateTime.UtcNow,
                AvailableDataSourceIds = dataSourceIds
            };
            _connections[connectionId] = updated;
        }
    }

    public bool IsAgentOnline(Guid tenantId)
    {
        return _tenantToConnection.ContainsKey(tenantId);
    }

    public GatewayAgentInfo? GetAgentInfo(Guid tenantId)
    {
        if (_tenantToConnection.TryGetValue(tenantId, out var connectionId)
            && _connections.TryGetValue(connectionId, out var info))
        {
            return info;
        }
        return null;
    }

    public string? GetConnectionId(Guid tenantId)
    {
        _tenantToConnection.TryGetValue(tenantId, out var connectionId);
        return connectionId;
    }
}
