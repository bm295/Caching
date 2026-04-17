using DistributedCache.Api.Hashing;
using DistributedCache.Api.Models;

namespace DistributedCache.Api.Tests;

public class RendezvousHashRingTests
{
    private static readonly PeerNode[] Nodes =
    [
        new("node-a", new Uri("http://node-a")),
        new("node-b", new Uri("http://node-b")),
        new("node-c", new Uri("http://node-c"))
    ];

    [Fact]
    public void SameKeyReturnsStablePlacement()
    {
        var ring = new RendezvousHashRing(Nodes);

        var first = ring.GetResponsibleNodes("user:42", 2).Select(x => x.NodeId).ToArray();
        var second = ring.GetResponsibleNodes("user:42", 2).Select(x => x.NodeId).ToArray();

        Assert.Equal(first, second);
    }

    [Fact]
    public void PlacementHonorsReplicaCount()
    {
        var ring = new RendezvousHashRing(Nodes);

        var owners = ring.GetResponsibleNodes("order:100", 2);

        Assert.Equal(2, owners.Count);
        Assert.Equal(owners.Select(x => x.NodeId).Distinct().Count(), owners.Count);
    }
}
