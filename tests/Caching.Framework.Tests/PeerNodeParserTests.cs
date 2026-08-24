using Caching.Framework.Models;
using Caching.Framework.Services;

namespace Caching.Framework.Tests;

public class PeerNodeParserTests
{
    [Fact]
    public void ParseSkipsMalformedPeersAndAddsLocalNode()
    {
        var options = new CacheClusterOptions
        {
            NodeId = "node-a",
            ReplicationFactor = 3,
            Peers =
            [
                "bad-entry",
                "node-b=http://node-b"
            ]
        };

        var nodes = PeerNodeParser.Parse(options);

        Assert.Equal(2, nodes.Count);
        Assert.Contains(nodes, node => node.NodeId == "node-a" && node.BaseAddress == new Uri("http://localhost:8080"));
        Assert.Contains(nodes, node => node.NodeId == "node-b" && node.BaseAddress == new Uri("http://node-b"));
    }
}
