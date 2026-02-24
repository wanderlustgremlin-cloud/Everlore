using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace Everlore.QueryEngine.Caching;

public class DistributedQueryCacheService(
    IDistributedCache cache,
    ILogger<DistributedQueryCacheService> logger) : IQueryCacheService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public async Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        var data = await cache.GetStringAsync(key, ct);
        if (data is null)
            return null;

        logger.LogDebug("Cache hit for key {CacheKey}", key);
        return JsonSerializer.Deserialize<T>(data, JsonOptions);
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class
    {
        var json = JsonSerializer.Serialize(value, JsonOptions);
        var options = new DistributedCacheEntryOptions();

        if (expiry.HasValue)
            options.AbsoluteExpirationRelativeToNow = expiry.Value;
        else
            options.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(30);

        await cache.SetStringAsync(key, json, options, ct);
        logger.LogDebug("Cached value for key {CacheKey}", key);
    }

    public async Task RemoveAsync(string key, CancellationToken ct = default)
    {
        await cache.RemoveAsync(key, ct);
        logger.LogDebug("Removed cache key {CacheKey}", key);
    }

    public Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default)
    {
        // IDistributedCache doesn't support prefix deletion natively.
        // For Garnet/Redis, this would require SCAN + DEL via IConnectionMultiplexer.
        // For now, individual keys must be removed explicitly.
        logger.LogWarning("RemoveByPrefixAsync is a no-op with IDistributedCache. Use explicit key removal.");
        return Task.CompletedTask;
    }

    public static string SchemaKey(Guid tenantId, Guid dataSourceId) =>
        $"schema:{tenantId}:{dataSourceId}";

    public static string QueryKey(Guid tenantId, Guid dataSourceId, string queryHash) =>
        $"query:{tenantId}:{dataSourceId}:{queryHash}";
}
