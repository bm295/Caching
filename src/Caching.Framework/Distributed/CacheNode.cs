namespace Caching.Framework.Distributed;

/// <summary>A logical cache node available to the distributed placement engine.</summary>
public sealed record CacheNode(string NodeId, Uri Endpoint);
