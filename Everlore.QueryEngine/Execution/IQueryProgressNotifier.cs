namespace Everlore.QueryEngine.Execution;

public interface IQueryProgressNotifier
{
    Task NotifyProgressAsync(Guid tenantId, string operationId, string stage, int? percentComplete = null, string? message = null, CancellationToken ct = default);
    Task NotifyCompletedAsync(Guid tenantId, string operationId, int rowCount, TimeSpan executionTime, CancellationToken ct = default);
    Task NotifyFailedAsync(Guid tenantId, string operationId, string error, CancellationToken ct = default);
}
