using DistributedCache.Api.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace DistributedCache.Api.Services;

public sealed class LocalCacheStore(
    IDistributedCache cache,
    IConfiguration configuration,
    IHostEnvironment hostEnvironment,
    ILogger<LocalCacheStore> logger) : ILocalCacheStore
{
    private readonly string _nodeId = configuration[$"{CacheClusterOptions.SectionName}:NodeId"]
                                      ?? hostEnvironment.ApplicationName;

    public async Task SetAsync(string key, string value, TimeSpan? ttl, CancellationToken cancellationToken)
    {
        var options = new DistributedCacheEntryOptions();

        if (ttl is { } expiration)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }

        await cache.SetStringAsync(key, value, options, cancellationToken);
    }

    public async Task<CacheReadResult?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var value = await cache.GetStringAsync(key, cancellationToken);
        if (value is null)
        {
            return null;
        }

        return new CacheReadResult(key, value, _nodeId, IsLocal: true);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            await cache.RemoveAsync(key, cancellationToken);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to remove cache key {Key}", key);
            return false;
        }
    }
}
