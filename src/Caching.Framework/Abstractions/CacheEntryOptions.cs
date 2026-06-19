namespace Caching.Framework.Abstractions;

/// <summary>Options for a single cache write.</summary>
public sealed record CacheEntryOptions
{
    public TimeSpan? TimeToLive { get; init; }
    public string? Region { get; init; }

    public static CacheEntryOptions NeverExpire { get; } = new();
    public static CacheEntryOptions WithTtl(TimeSpan ttl) => new() { TimeToLive = ttl };
}
