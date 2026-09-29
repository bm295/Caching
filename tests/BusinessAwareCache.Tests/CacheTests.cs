using BusinessAwareCache;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace BusinessAwareCache.Tests;

public class CacheTests
{
    private static CacheService Create() => new(new MemoryCache(new MemoryCacheOptions()), new DefaultCacheKeyBuilder(), new CachePolicyRegistry());
    private static readonly CachePolicy Policy = new(TimeSpan.FromMinutes(1), "v1", ["catalog"]);

    [Fact]
    public async Task Registered_policy_is_used()
    {
        var services = new ServiceCollection();
        services.AddBusinessAwareCache(p => p.Add<string>(Policy));
        using var provider = services.BuildServiceProvider();
        var cache = provider.GetRequiredService<ICacheService>();
        Assert.Equal("ok", await cache.GetOrCreateAsync("x", _ => Task.FromResult<string?>("ok")));
    }

    [Fact]
    public async Task Tag_removal_during_load_prevents_publication()
    {
        var cache = Create();
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var loading = cache.GetOrCreateAsync<string>("x", async _ =>
        {
            started.SetResult();
            await release.Task;
            return "old";
        }, Policy);
        await started.Task;
        cache.RemoveTag("catalog");
        release.SetResult();
        Assert.Equal("old", await loading);
        Assert.Equal("new", await cache.GetOrCreateAsync("x", _ => Task.FromResult<string?>("new"), Policy));
    }

    [Fact]
    public async Task Hit_and_key_isolation()
    {
        var cache = Create();
        var calls = 0;
        Task<string?> Factory(CancellationToken _) => Task.FromResult<string?>(($"value-{++calls}"));
        Assert.Equal("value-1", await cache.GetOrCreateAsync("1", Factory, Policy));
        Assert.Equal("value-1", await cache.GetOrCreateAsync("1", Factory, Policy));
        Assert.Equal("value-2", await cache.GetOrCreateAsync("1", Factory, Policy with { Version = "v2" }));
        Assert.Equal(2, calls);
        var keys = new DefaultCacheKeyBuilder();
        Assert.NotEqual(keys.BuildKey(typeof(string), "v1", "23"), keys.BuildKey(typeof(string), "v12", "3"));
        Assert.NotEqual(keys.BuildKey(typeof(string), "v1", "1"), keys.BuildKey(typeof(int), "v1", "1"));
    }

    [Fact]
    public async Task Ttl_expires_and_invalid_ttl_throws()
    {
        var cache = Create();
        var calls = 0;
        Task<int?> Factory(CancellationToken _) => Task.FromResult<int?>(++calls);
        var policy = new CachePolicy(TimeSpan.FromMilliseconds(40));
        Assert.Equal(1, await cache.GetOrCreateAsync("x", Factory, policy));
        await Task.Delay(100);
        Assert.Equal(2, await cache.GetOrCreateAsync("x", Factory, policy));
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => cache.GetOrCreateAsync("x", Factory, new CachePolicy(TimeSpan.Zero)));
    }

    [Fact]
    public async Task Tag_and_key_removal()
    {
        var cache = Create();
        var calls = 0;
        Task<int?> Factory(CancellationToken _) => Task.FromResult<int?>(++calls);
        await cache.GetOrCreateAsync("x", Factory, Policy);
        cache.RemoveTag("catalog");
        Assert.Equal(2, await cache.GetOrCreateAsync("x", Factory, Policy));
        cache.Remove<int?>("x", "v1");
        Assert.Equal(3, await cache.GetOrCreateAsync("x", Factory, Policy));
    }

    [Fact]
    public async Task Null_and_failure_are_not_cached()
    {
        var cache = Create();
        var calls = 0;
        Assert.Null(await cache.GetOrCreateAsync<string>("x", _ => Task.FromResult<string?>(null), Policy));
        await Assert.ThrowsAsync<InvalidOperationException>(() => cache.GetOrCreateAsync<string>("x", _ => throw new InvalidOperationException(), Policy));
        Assert.Equal("ok", await cache.GetOrCreateAsync("x", _ => { calls++; return Task.FromResult<string?>("ok"); }, Policy));
        Assert.Equal(1, calls);
    }

    [Fact]
    public async Task Concurrent_same_key_loads_once()
    {
        var cache = Create();
        var calls = 0;
        var tasks = Enumerable.Range(0, 20).Select(_ => cache.GetOrCreateAsync("x", async (CancellationToken _) =>
        {
            Interlocked.Increment(ref calls);
            await Task.Delay(30);
            return "ok";
        }, Policy));
        Assert.All(await Task.WhenAll(tasks), value => Assert.Equal("ok", value));
        Assert.Equal(1, calls);
    }
}
