namespace Caching.Framework.Distributed;

public sealed class DistributedCacheOptions
{
    public string LocalNodeId { get; set; } = Environment.MachineName;
    public int ReplicationFactor { get; set; } = 1;
    public List<CacheNode> Nodes { get; set; } = [];
}
