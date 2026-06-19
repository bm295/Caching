namespace Caching.Framework.Abstractions;

/// <summary>Represents a cache hit and its metadata.</summary>
public sealed record CacheItem<T>(string Key, T Value, DateTimeOffset? ExpiresAt = null, string? Region = null);
