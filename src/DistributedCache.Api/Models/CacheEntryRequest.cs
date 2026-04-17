namespace DistributedCache.Api.Models;

public sealed record CacheEntryRequest(string Value, int? TtlSeconds);
