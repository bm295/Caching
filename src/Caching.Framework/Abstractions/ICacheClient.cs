namespace Caching.Framework.Abstractions;

/// <summary>Application-facing cache API used by framework consumers.</summary>
public interface ICacheClient
{
    Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    Task<CacheItem<T>?> GetAsync<T>(string key, string? region = null, CancellationToken cancellationToken = default);
    Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default);
    Task<bool> RemoveAsync(string key, string? region = null, CancellationToken cancellationToken = default);
}
