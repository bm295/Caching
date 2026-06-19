using Caching.Framework.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace Caching.Framework.Memory;

/// <summary>Thread-safe in-process cache client backed by <see cref="IMemoryCache" />.</summary>
public sealed class MemoryCacheClient(IMemoryCache memoryCache) : ICacheClient
{
    public Task SetAsync<T>(string key, T value, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();

        options ??= CacheEntryOptions.NeverExpire;
        var expiresAt = options.TimeToLive is { } ttl ? DateTimeOffset.UtcNow.Add(ttl) : (DateTimeOffset?)null;
        var entry = new CacheItem<T>(key, value, expiresAt, options.Region);
        var memoryOptions = new MemoryCacheEntryOptions();
        if (options.TimeToLive is { } timeToLive)
        {
            memoryOptions.AbsoluteExpirationRelativeToNow = timeToLive;
        }

        memoryCache.Set(BuildKey(key, options.Region), entry, memoryOptions);
        return Task.CompletedTask;
    }

    public Task<CacheItem<T>?> GetAsync<T>(string key, string? region = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(memoryCache.TryGetValue<CacheItem<T>>(BuildKey(key, region), out var item) ? item : null);
    }

    public async Task<T> GetOrCreateAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CacheEntryOptions? options = null, CancellationToken cancellationToken = default)
    {
        var cached = await GetAsync<T>(key, options?.Region, cancellationToken);
        if (cached is not null)
        {
            return cached.Value;
        }

        var value = await factory(cancellationToken);
        await SetAsync(key, value, options, cancellationToken);
        return value;
    }

    public Task<bool> RemoveAsync(string key, string? region = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        cancellationToken.ThrowIfCancellationRequested();
        memoryCache.Remove(BuildKey(key, region));
        return Task.FromResult(true);
    }

    private static string BuildKey(string key, string? region) => string.IsNullOrWhiteSpace(region) ? key : $"{region}:{key}";
}
