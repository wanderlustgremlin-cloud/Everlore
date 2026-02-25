using Everlore.QueryEngine.Execution;

namespace Everlore.Gateway.Connections;

public class NoOpProgressNotifier : IQueryProgressNotifier
{
    public Task NotifyProgressAsync(Guid tenantId, string operationId, string stage,
        int? percentComplete = null, string? message = null, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task NotifyCompletedAsync(Guid tenantId, string operationId, int rowCount,
        TimeSpan executionTime, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task NotifyFailedAsync(Guid tenantId, string operationId, string error,
        CancellationToken ct = default)
        => Task.CompletedTask;
}
