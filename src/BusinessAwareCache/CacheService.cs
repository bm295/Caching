using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessAwareCache;

public sealed record CachePolicy(TimeSpan TimeToLive, string Version = "v1", IReadOnlyCollection<string>? Tags = null);

public interface ICacheKeyBuilder
{
    string BuildKey(Type dataType, string version, string identifier);
}

public sealed class DefaultCacheKeyBuilder : ICacheKeyBuilder
{
    public string BuildKey(Type dataType, string version, string identifier)
    {
        ArgumentNullException.ThrowIfNull(dataType);
        if (string.IsNullOrWhiteSpace(version)) throw new ArgumentException("Version is required.", nameof(version));
        if (string.IsNullOrWhiteSpace(identifier)) throw new ArgumentException("Identifier is required.", nameof(identifier));
        var type = dataType.AssemblyQualifiedName ?? dataType.FullName ?? dataType.Name;
        return $"{type.Length}:{type}{version.Length}:{version}{identifier.Length}:{identifier}";
    }
}

public interface ICacheService
{
    Task<T?> GetOrCreateAsync<T>(string identifier, Func<CancellationToken, Task<T?>> factory,
        CachePolicy? policy = null, CancellationToken cancellationToken = default);
    void Remove<T>(string identifier, string? version = null);
    void RemoveTag(string tag);
}

public sealed class CachePolicyRegistry
{
    private readonly Dictionary<Type, CachePolicy> _policies = new();
    public CachePolicyRegistry Add<T>(CachePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        CacheService.Validate(policy);
        _policies[typeof(T)] = policy;
        return this;
    }
    internal CachePolicy Get<T>() => _policies.TryGetValue(typeof(T), out var policy)
        ? policy : throw new InvalidOperationException($"No cache policy registered for {typeof(T)}.");
}

public static class CacheServiceCollectionExtensions
{
    public static IServiceCollection AddBusinessAwareCache(this IServiceCollection services,
        Action<CachePolicyRegistry> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);
        var registry = new CachePolicyRegistry();
        configure(registry);
        services.AddMemoryCache();
        services.AddSingleton(registry);
        services.AddSingleton<ICacheKeyBuilder, DefaultCacheKeyBuilder>();
        services.AddSingleton<ICacheService, CacheService>();
        return services;
    }
}

public sealed class CacheService(IMemoryCache cache, ICacheKeyBuilder keyBuilder, CachePolicyRegistry policies) : ICacheService
{
    private sealed record Entry(object Value, string[] Tags);
    private sealed class KeyLock { public readonly SemaphoreSlim Semaphore = new(1, 1); public int Users; }
    private readonly object _gate = new();
    private readonly Dictionary<string, KeyLock> _locks = new();
    private readonly Dictionary<string, HashSet<string>> _tagKeys = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _tagGeneration = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string[]> _keyTags = new();
    private static readonly Meter Meter = new("BusinessAwareCache", "0.1.0");
    private static readonly Counter<long> Hits = Meter.CreateCounter<long>("cache.hits");
    private static readonly Counter<long> Misses = Meter.CreateCounter<long>("cache.misses");
    private static readonly Histogram<double> LoadMs = Meter.CreateHistogram<double>("cache.load.duration", "ms");

    public async Task<T?> GetOrCreateAsync<T>(string identifier, Func<CancellationToken, Task<T?>> factory,
        CachePolicy? policy = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(factory);
        policy ??= policies.Get<T>();
        Validate(policy);
        var key = keyBuilder.BuildKey(typeof(T), policy.Version, identifier);
        cancellationToken.ThrowIfCancellationRequested();
        lock (_gate)
        {
            if (cache.TryGetValue(key, out Entry? hit) && hit is not null)
            { Hits.Add(1); return (T)hit.Value; }
        }
        KeyLock keyLock;
        lock (_gate)
        {
            if (!_locks.TryGetValue(key, out keyLock!)) _locks[key] = keyLock = new KeyLock();
            keyLock.Users++;
        }
        try
        {
            await keyLock.Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                Dictionary<string, long> generations;
                var tags = (policy.Tags ?? Array.Empty<string>()).Distinct(StringComparer.Ordinal).ToArray();
                lock (_gate)
                {
                    if (cache.TryGetValue(key, out Entry? hit) && hit is not null)
                    { Hits.Add(1); return (T)hit.Value; }
                    generations = tags.ToDictionary(t => t, t => _tagGeneration.GetValueOrDefault(t), StringComparer.Ordinal);
                }
                Misses.Add(1);
                var start = Stopwatch.GetTimestamp();
                T? value;
                try { value = await factory(cancellationToken).ConfigureAwait(false); }
                finally { LoadMs.Record(Stopwatch.GetElapsedTime(start).TotalMilliseconds); }
                if (value is null) return default;
                lock (_gate)
                {
                    if (tags.Any(t => _tagGeneration.GetValueOrDefault(t) != generations[t])) return value;
                    RemoveKey(key);
                    cache.Set(key, new Entry(value, tags), policy.TimeToLive);
                    _keyTags[key] = tags;
                    foreach (var tag in tags)
                    {
                        if (!_tagKeys.TryGetValue(tag, out var keys)) _tagKeys[tag] = keys = new HashSet<string>();
                        keys.Add(key);
                    }
                }
                return value;
            }
            finally { keyLock.Semaphore.Release(); }
        }
        finally
        {
            lock (_gate)
            {
                if (--keyLock.Users == 0) { _locks.Remove(key); keyLock.Semaphore.Dispose(); }
            }
        }
    }

    public void Remove<T>(string identifier, string? version = null)
    {
        version ??= policies.Get<T>().Version;
        var key = keyBuilder.BuildKey(typeof(T), version, identifier);
        lock (_gate) RemoveKey(key);
    }

    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) throw new ArgumentException("Tag is required.", nameof(tag));
        lock (_gate)
        {
            _tagGeneration[tag] = _tagGeneration.GetValueOrDefault(tag) + 1;
            if (_tagKeys.TryGetValue(tag, out var keys))
                foreach (var key in keys.ToArray()) RemoveKey(key);
        }
    }

    private void RemoveKey(string key)
    {
        cache.Remove(key);
        if (!_keyTags.Remove(key, out var tags)) return;
        foreach (var tag in tags)
        {
            if (_tagKeys.TryGetValue(tag, out var keys))
            {
                keys.Remove(key);
                if (keys.Count == 0) _tagKeys.Remove(tag);
            }
        }
    }

    internal static void Validate(CachePolicy policy)
    {
        if (policy.TimeToLive <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(policy), "TTL must be positive.");
        if (string.IsNullOrWhiteSpace(policy.Version)) throw new ArgumentException("Version is required.", nameof(policy));
        if (policy.Tags?.Any(string.IsNullOrWhiteSpace) == true) throw new ArgumentException("Tags cannot be empty.", nameof(policy));
    }
}
