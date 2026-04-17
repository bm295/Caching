using DistributedCache.Api.Models;
using Microsoft.Extensions.Caching.Memory;
using StackExchange.Redis;

namespace DistributedCache.Api.Services;

public sealed class LocalCacheStore : ILocalCacheStore
{
    private const string KeyPrefix = "distributed-cache:";

    private readonly string _nodeId;
    private readonly IMemoryCache _memoryCache;
    private readonly IDatabase? _redisDatabase;
    private readonly ILogger<LocalCacheStore> _logger;

    public LocalCacheStore(
        IMemoryCache memoryCache,
        IConfiguration configuration,
        IHostEnvironment hostEnvironment,
        ILogger<LocalCacheStore> logger,
        IConnectionMultiplexer? redisConnection = null)
    {
        _memoryCache = memoryCache;
        _logger = logger;
        _nodeId = configuration[$"{CacheClusterOptions.SectionName}:NodeId"]
                  ?? hostEnvironment.ApplicationName;
        _redisDatabase = redisConnection?.GetDatabase();
    }

    public async Task SetAsync(string key, string value, TimeSpan? ttl, CancellationToken cancellationToken)
    {
        if (_redisDatabase is not null)
        {
            await _redisDatabase.StringSetAsync(NormalizeKey(key), value, ttl);
            return;
        }

        var options = new MemoryCacheEntryOptions();
        if (ttl is { } expiration)
        {
            options.AbsoluteExpirationRelativeToNow = expiration;
        }

        _memoryCache.Set(key, value, options);
        await Task.CompletedTask;
    }

    public async Task<CacheReadResult?> GetAsync(string key, CancellationToken cancellationToken)
    {
        if (_redisDatabase is not null)
        {
            var value = await _redisDatabase.StringGetAsync(NormalizeKey(key));
            if (!value.HasValue)
            {
                return null;
            }

            return new CacheReadResult(key, value.ToString(), _nodeId, IsLocal: true);
        }

        if (!_memoryCache.TryGetValue<string>(key, out var memoryValue) || memoryValue is null)
        {
            return null;
        }

        return new CacheReadResult(key, memoryValue, _nodeId, IsLocal: true);
    }

    public async Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            if (_redisDatabase is not null)
            {
                return await _redisDatabase.KeyDeleteAsync(NormalizeKey(key));
            }

            _memoryCache.Remove(key);
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to remove cache key {Key}", key);
            return false;
        }
    }

    private static RedisKey NormalizeKey(string key)
        => (RedisKey)$"{KeyPrefix}{key}";
}
