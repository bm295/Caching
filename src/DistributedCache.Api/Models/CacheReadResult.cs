namespace DistributedCache.Api.Models;

public sealed record CacheReadResult(string Key, string Value, string NodeId, bool IsLocal);
