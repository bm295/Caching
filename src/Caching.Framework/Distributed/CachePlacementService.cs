using Microsoft.Extensions.Options;

namespace Caching.Framework.Distributed;

/// <summary>Exposes deterministic owner selection for distributed cache implementations.</summary>
public sealed class CachePlacementService
{
    private readonly DistributedCacheOptions _options;
    private readonly RendezvousHashRing _ring;

    public CachePlacementService(IOptions<DistributedCacheOptions> options)
    {
        _options = options.Value;
        var nodes = _options.Nodes.Count > 0
            ? _options.Nodes
            : [new CacheNode(_options.LocalNodeId, new Uri("http://localhost"))];
        _ring = new RendezvousHashRing(nodes);
    }

    public IReadOnlyList<CacheNode> GetOwners(string key) => _ring.GetOwners(key, _options.ReplicationFactor);
    public bool IsLocalOwner(string key) => GetOwners(key).Any(n => string.Equals(n.NodeId, _options.LocalNodeId, StringComparison.OrdinalIgnoreCase));
}
