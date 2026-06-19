using Caching.Framework.Abstractions;
using Caching.Framework.Memory;
using Microsoft.Extensions.Caching.Memory;

namespace Caching.Framework.Tests;

public sealed class MemoryCacheClientTests
{
    [Fact]
    public async Task GetOrCreateCachesFactoryResult()
    {
        var client = new MemoryCacheClient(new MemoryCache(new MemoryCacheOptions()));
        var calls = 0;

        var first = await client.GetOrCreateAsync("customer:42", _ => Task.FromResult(++calls), CacheEntryOptions.WithTtl(TimeSpan.FromMinutes(5)));
        var second = await client.GetOrCreateAsync("customer:42", _ => Task.FromResult(++calls), CacheEntryOptions.WithTtl(TimeSpan.FromMinutes(5)));

        Assert.Equal(1, first);
        Assert.Equal(1, second);
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task RegionSeparatesKeys()
    {
        var client = new MemoryCacheClient(new MemoryCache(new MemoryCacheOptions()));

        await client.SetAsync("settings", "tenant-a", new CacheEntryOptions { Region = "a" });
        await client.SetAsync("settings", "tenant-b", new CacheEntryOptions { Region = "b" });

        Assert.Equal("tenant-a", (await client.GetAsync<string>("settings", "a"))?.Value);
        Assert.Equal("tenant-b", (await client.GetAsync<string>("settings", "b"))?.Value);
    }
}
