namespace Caching.Framework.Models;

public sealed record CacheEntryRequest(string Value, int? TtlSeconds);
