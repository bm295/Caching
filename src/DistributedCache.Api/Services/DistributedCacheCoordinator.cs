using DistributedCache.Api.Hashing;
using DistributedCache.Api.Models;
using Microsoft.Extensions.Options;

namespace DistributedCache.Api.Services;

public sealed class DistributedCacheCoordinator
{
    private readonly CacheClusterOptions _options;
    private readonly ILocalCacheStore _localCacheStore;
    private readonly IPeerNodeClient _peerNodeClient;
    private readonly ILogger<DistributedCacheCoordinator> _logger;
    private readonly RendezvousHashRing _ring;
    private readonly string _nodeId;

    public DistributedCacheCoordinator(
        IOptions<CacheClusterOptions> options,
        ILocalCacheStore localCacheStore,
        IPeerNodeClient peerNodeClient,
        ILogger<DistributedCacheCoordinator> logger)
    {
        _options = options.Value;
        _localCacheStore = localCacheStore;
        _peerNodeClient = peerNodeClient;
        _logger = logger;

        var nodes = ParsePeers(_options);
        _ring = new RendezvousHashRing(nodes);
        _nodeId = _options.NodeId;
    }

    public IReadOnlyList<PeerNode> GetPlacement(string key)
        => _ring.GetResponsibleNodes(key, _options.ReplicationFactor);

    public async Task SetAsync(string key, string value, TimeSpan? ttl, CancellationToken cancellationToken)
    {
        var owners = GetPlacement(key);

        var tasks = owners.Select(async node =>
        {
            if (IsLocal(node))
            {
                await _localCacheStore.SetAsync(key, value, ttl, cancellationToken);
                return;
            }

            var success = await _peerNodeClient.SetAsync(node, key, value, ttl, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Replica write failed for key {Key} on node {NodeId}", key, node.NodeId);
            }
        });

        await Task.WhenAll(tasks);
    }

    public async Task<CacheReadResult?> GetAsync(string key, CancellationToken cancellationToken)
    {
        var owners = GetPlacement(key);

        foreach (var node in owners)
        {
            if (IsLocal(node))
            {
                var localValue = await _localCacheStore.GetAsync(key, cancellationToken);
                if (localValue is not null)
                {
                    return localValue;
                }

                continue;
            }

            var remoteValue = await _peerNodeClient.GetAsync(node, key, cancellationToken);
            if (remoteValue is not null)
            {
                return remoteValue with { IsLocal = false };
            }
        }

        return null;
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken)
    {
        var owners = GetPlacement(key);

        var tasks = owners.Select(async node =>
        {
            if (IsLocal(node))
            {
                await _localCacheStore.DeleteAsync(key, cancellationToken);
                return;
            }

            var success = await _peerNodeClient.DeleteAsync(node, key, cancellationToken);
            if (!success)
            {
                _logger.LogWarning("Replica delete failed for key {Key} on node {NodeId}", key, node.NodeId);
            }
        });

        await Task.WhenAll(tasks);
    }

    private bool IsLocal(PeerNode node)
        => string.Equals(node.NodeId, _nodeId, StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<PeerNode> ParsePeers(CacheClusterOptions options)
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
