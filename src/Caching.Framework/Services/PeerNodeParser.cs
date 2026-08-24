using Caching.Framework.Models;

namespace Caching.Framework.Services;

public static class PeerNodeParser
{
    public static IReadOnlyList<PeerNode> Parse(CacheClusterOptions options)
    {
        var peers = new List<PeerNode>();

        foreach (var entry in options.Peers)
        {
            var parts = entry.Split('=', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !Uri.TryCreate(parts[1], UriKind.Absolute, out var uri))
            {
                continue;
            }

            peers.Add(new PeerNode(parts[0], uri));
        }

        if (!peers.Any(p => string.Equals(p.NodeId, options.NodeId, StringComparison.OrdinalIgnoreCase)))
        {
            peers.Add(new PeerNode(options.NodeId, new Uri("http://localhost:8080")));
        }

        return peers;
    }
}
