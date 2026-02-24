namespace Everlore.Api.Hubs;

public interface IQueryHubClient
{
    Task QueryProgress(QueryProgressMessage message);
    Task SchemaDiscoveryProgress(QueryProgressMessage message);
    Task QueryCompleted(QueryCompletedMessage message);
    Task QueryFailed(QueryFailedMessage message);
}

public record QueryProgressMessage(
    string OperationId,
    string Stage,
    int? PercentComplete,
    string? Message);

public record QueryCompletedMessage(
    string OperationId,
    int RowCount,
    double ExecutionTimeMs);

public record QueryFailedMessage(
    string OperationId,
    string Error);
