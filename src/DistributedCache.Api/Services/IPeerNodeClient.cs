using DistributedCache.Api.Models;

namespace DistributedCache.Api.Services;

public interface IPeerNodeClient
{
    Task<bool> SetAsync(PeerNode peer, string key, string value, TimeSpan? ttl, CancellationToken cancellationToken);

    Task<CacheReadResult?> GetAsync(PeerNode peer, string key, CancellationToken cancellationToken);

    Task<bool> DeleteAsync(PeerNode peer, string key, CancellationToken cancellationToken);
}
