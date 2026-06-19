using System.Security.Cryptography;
using System.Text;

namespace Caching.Framework.Distributed;

/// <summary>Chooses stable cache owners with highest-random-weight rendezvous hashing.</summary>
public sealed class RendezvousHashRing
{
    private readonly IReadOnlyList<CacheNode> _nodes;

    public RendezvousHashRing(IEnumerable<CacheNode> nodes)
    {
        _nodes = nodes.GroupBy(n => n.NodeId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(n => n.NodeId, StringComparer.Ordinal)
            .ToArray();

        if (_nodes.Count == 0)
        {
            throw new InvalidOperationException("At least one cache node is required.");
        }
    }

    public IReadOnlyList<CacheNode> GetOwners(string key, int replicas = 1)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        var take = Math.Clamp(replicas, 1, _nodes.Count);
        return _nodes.Select(node => new { Node = node, Score = Score(node.NodeId, key) })
            .OrderByDescending(x => x.Score)
            .Take(take)
            .Select(x => x.Node)
            .ToArray();
    }

    private static ulong Score(string nodeId, string key)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{nodeId}:{key}"));
        return BitConverter.ToUInt64(bytes, 0);
    }
}
