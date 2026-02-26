namespace Everlore.Application.Common.Interfaces;

public interface IExploreService
{
    Task<IReadOnlyList<Dictionary<string, object?>>> ExploreAsync(
        Guid dataSourceId, int dataSourceType, string sql, CancellationToken ct = default);
}
