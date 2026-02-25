using Everlore.QueryEngine.Caching;

namespace Everlore.Gateway.Connections;

/// <summary>
/// No-op cache for the gateway agent — results are cached on the server side,
/// not locally on the agent.
/// </summary>
public class NoOpCacheService : IQueryCacheService
{
    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
        => Task.FromResult<T?>(null);

    public Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
        => Task.CompletedTask;

    public Task RemoveAsync(string key, CancellationToken ct = default)
        => Task.CompletedTask;

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
        => Task.CompletedTask;
}
