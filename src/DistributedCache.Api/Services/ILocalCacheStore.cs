using DistributedCache.Api.Models;

namespace DistributedCache.Api.Services;

public interface ILocalCacheStore
{
    Task SetAsync(string key, string value, TimeSpan? ttl, CancellationToken cancellationToken);

    Task<CacheReadResult?> GetAsync(string key, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(string key, CancellationToken cancellationToken);
}
