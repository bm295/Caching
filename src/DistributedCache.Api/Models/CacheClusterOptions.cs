namespace DistributedCache.Api.Models;

public sealed class CacheClusterOptions
{
    public const string SectionName = "CacheCluster";

    public string NodeId { get; set; } = "node-a";

    public int ReplicationFactor { get; set; } = 2;

    public int RequestTimeoutMilliseconds { get; set; } = 1500;

    public List<string> Peers { get; set; } = [];
}
