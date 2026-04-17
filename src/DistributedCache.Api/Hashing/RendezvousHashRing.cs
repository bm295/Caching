using System.Security.Cryptography;
using System.Text;
using DistributedCache.Api.Models;

namespace DistributedCache.Api.Hashing;

public sealed class RendezvousHashRing
{
    private readonly IReadOnlyList<PeerNode> _nodes;

    public RendezvousHashRing(IEnumerable<PeerNode> nodes)
    {
        _nodes = nodes
            .GroupBy(n => n.NodeId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(n => n.NodeId, StringComparer.Ordinal)
            .ToArray();

        if (_nodes.Count == 0)
        {
            throw new InvalidOperationException("At least one node is required for rendezvous hashing.");
        }
    }

    public IReadOnlyList<PeerNode> GetResponsibleNodes(string key, int replicas)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key must not be null or empty.", nameof(key));
        }

        var take = Math.Clamp(replicas, 1, _nodes.Count);

        return _nodes
            .Select(node => new { Node = node, Score = ComputeScore(node.NodeId, key) })
            .OrderByDescending(x => x.Score)
            .Take(take)
            .Select(x => x.Node)
            .ToArray();
    }

    private static ulong ComputeScore(string nodeId, string key)
    {
        var input = $"{nodeId}:{key}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));

        return BitConverter.ToUInt64(bytes, 0);
    }
}
