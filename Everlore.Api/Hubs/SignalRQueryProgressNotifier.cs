using Everlore.QueryEngine.Execution;
using Microsoft.AspNetCore.SignalR;

namespace Everlore.Api.Hubs;

public class SignalRQueryProgressNotifier(IHubContext<QueryHub, IQueryHubClient> hubContext) : IQueryProgressNotifier
{
    public async Task NotifyProgressAsync(Guid tenantId, string operationId, string stage,
        int? percentComplete = null, string? message = null, CancellationToken ct = default)
    {
        await hubContext.Clients
            .Group($"tenant:{tenantId}")
            .QueryProgress(new QueryProgressMessage(operationId, stage, percentComplete, message));
    }

    public async Task NotifyCompletedAsync(Guid tenantId, string operationId, int rowCount,
        TimeSpan executionTime, CancellationToken ct = default)
    {
        await hubContext.Clients
            .Group($"tenant:{tenantId}")
            .QueryCompleted(new QueryCompletedMessage(operationId, rowCount, executionTime.TotalMilliseconds));
    }

    public async Task NotifyFailedAsync(Guid tenantId, string operationId, string error,
        CancellationToken ct = default)
    {
        await hubContext.Clients
            .Group($"tenant:{tenantId}")
            .QueryFailed(new QueryFailedMessage(operationId, error));
    }
}
