using DistributedCache.Api.Models;
using DistributedCache.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace DistributedCache.Api.Tests;

public class DistributedCacheCoordinatorTests
{
    [Fact]
    public async Task SetWritesToLocalAndReplicaNodes()
    {
        var localStore = new FakeLocalCacheStore();
        var peerClient = new FakePeerNodeClient();

        var options = Options.Create(new CacheClusterOptions
        {
            NodeId = "node-a",
            ReplicationFactor = 3,
            Peers =
            [
                "node-a=http://node-a",
                "node-b=http://node-b",
                "node-c=http://node-c"
            ]
        });

        var sut = new DistributedCacheCoordinator(options, localStore, peerClient, NullLogger<DistributedCacheCoordinator>.Instance);

        await sut.SetAsync("k1", "v1", TimeSpan.FromSeconds(30), CancellationToken.None);

        Assert.Contains(localStore.Keys, k => k == "k1");
        Assert.True(peerClient.SetCalls >= 1);
    }

    [Fact]
    public async Task InspectReplicasReturnsReplicaStateForEachOwner()
    {
        var localStore = new FakeLocalCacheStore();
        var peerClient = new FakePeerNodeClient();

        var options = Options.Create(new CacheClusterOptions
        {
            NodeId = "node-a",
            ReplicationFactor = 3,
            Peers =
            [
                "node-a=http://node-a",
                "node-b=http://node-b",
                "node-c=http://node-c"
            ]
        });

        await localStore.SetAsync("k1", "local-value", TimeSpan.FromSeconds(30), CancellationToken.None);
        peerClient.ReadResponses["node-b:k1"] = new CacheReadResult("k1", "remote-b", "node-b", false);
        peerClient.ReadResponses["node-c:k1"] = new CacheReadResult("k1", "remote-c", "node-c", false);

        var sut = new DistributedCacheCoordinator(options, localStore, peerClient, NullLogger<DistributedCacheCoordinator>.Instance);

        var replicas = await sut.InspectReplicasAsync("k1", CancellationToken.None);

        Assert.Equal(3, replicas.Count);
        Assert.Contains(replicas, r => r.NodeId == "node-a" && r.IsLocalNode && r.HasValue && r.Value == "local-value");
        Assert.Contains(replicas, r => r.NodeId == "node-b" && !r.IsLocalNode && r.HasValue && r.Value == "remote-b");
        Assert.Contains(replicas, r => r.NodeId == "node-c" && !r.IsLocalNode && r.HasValue && r.Value == "remote-c");
    }

    private sealed class FakeLocalCacheStore : ILocalCacheStore
    {
        private readonly Dictionary<string, string> _items = new(StringComparer.Ordinal);
        public IReadOnlyCollection<string> Keys => _items.Keys;

        public Task SetAsync(string key, string value, TimeSpan? ttl, CancellationToken cancellationToken)
        {
            _items[key] = value;
            return Task.CompletedTask;
        }

        public Task<CacheReadResult?> GetAsync(string key, CancellationToken cancellationToken)
        {
            return Task.FromResult(_items.TryGetValue(key, out var value)
                ? new CacheReadResult(key, value, "node-a", true)
                : null);
        }

        public Task<bool> DeleteAsync(string key, CancellationToken cancellationToken)
        {
            _items.Remove(key);
            return Task.FromResult(true);
        }
    }

    private sealed class FakePeerNodeClient : IPeerNodeClient
    {
        public int SetCalls { get; private set; }
        public Dictionary<string, CacheReadResult> ReadResponses { get; } = new(StringComparer.OrdinalIgnoreCase);

        public Task<bool> SetAsync(PeerNode peer, string key, string value, TimeSpan? ttl, CancellationToken cancellationToken)
        {
            SetCalls++;
            return Task.FromResult(true);
        }

        public Task<CacheReadResult?> GetAsync(PeerNode peer, string key, CancellationToken cancellationToken)
            => Task.FromResult(
                ReadResponses.TryGetValue($"{peer.NodeId}:{key}", out var value)
                    ? value
                    : null);

        public Task<bool> DeleteAsync(PeerNode peer, string key, CancellationToken cancellationToken)
            => Task.FromResult(true);
    }
}
