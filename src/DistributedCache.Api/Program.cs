using DistributedCache.Api.Models;
using DistributedCache.Api.Services;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddOptions<CacheClusterOptions>()
    .Bind(builder.Configuration.GetSection(CacheClusterOptions.SectionName))
    .Validate(o => !string.IsNullOrWhiteSpace(o.NodeId), "NodeId is required")
    .Validate(o => o.ReplicationFactor > 0, "ReplicationFactor must be > 0")
    .ValidateOnStart();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnectionString));
}

builder.Services.AddMemoryCache();
builder.Services.AddHttpClient(nameof(PeerNodeClient));
builder.Services.AddSingleton<ILocalCacheStore, LocalCacheStore>();
builder.Services.AddSingleton<IPeerNodeClient, PeerNodeClient>();
builder.Services.AddSingleton<DistributedCacheCoordinator>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapGet("/health/live", () => Results.Ok(new
{
    status = "ok",
    timestampUtc = DateTime.UtcNow
}));

app.MapGet("/demo/api/overview", (
    DistributedCacheCoordinator coordinator,
    IOptions<CacheClusterOptions> options,
    IServiceProvider services) =>
{
    var clusterOptions = options.Value;
    var usesRedis = services.GetService<IConnectionMultiplexer>() is not null;

    return Results.Ok(new
    {
        nodeId = clusterOptions.NodeId,
        replicationFactor = clusterOptions.ReplicationFactor,
        requestTimeoutMilliseconds = clusterOptions.RequestTimeoutMilliseconds,
        storageMode = usesRedis ? "redis" : "memory-fallback",
        storageLibrary = usesRedis ? "StackExchange.Redis" : "IMemoryCache",
        nodes = coordinator.GetClusterNodes()
            .Select(node => new
            {
                node.NodeId,
                url = node.BaseAddress.ToString(),
                isCurrent = string.Equals(node.NodeId, clusterOptions.NodeId, StringComparison.OrdinalIgnoreCase)
            }),
        features = new[]
        {
            new
            {
                name = "ConnectionMultiplexer.Connect",
                description = "Creates the shared Redis connection used by the local cache store."
            },
            new
            {
                name = "IDatabase.StringSetAsync",
                description = "Writes string values with an optional TTL."
            },
            new
            {
                name = "IDatabase.StringGetAsync",
                description = "Reads string values back from Redis."
            },
            new
            {
                name = "IDatabase.KeyDeleteAsync",
                description = "Deletes cache entries by key."
            }
        }
    });
});

app.MapGet("/demo/api/inspect/{key}", async (string key, DistributedCacheCoordinator coordinator, CancellationToken cancellationToken) =>
{
    var readResult = await coordinator.GetAsync(key, cancellationToken);
    var replicas = await coordinator.InspectReplicasAsync(key, cancellationToken);

    return Results.Ok(new
    {
        key,
        readResult,
        replicas
    });
});

app.MapGet("/cluster/placement/{key}", (string key, DistributedCacheCoordinator coordinator) =>
{
    var owners = coordinator.GetPlacement(key)
        .Select(x => new { x.NodeId, Url = x.BaseAddress.ToString() });

    return Results.Ok(new { key, owners });
});

app.MapPut("/cache/{key}", async (string key, CacheEntryRequest request, DistributedCacheCoordinator coordinator, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Value))
    {
        return Results.BadRequest(new { message = "Value is required." });
    }

    TimeSpan? ttl = request.TtlSeconds is > 0 ? TimeSpan.FromSeconds(request.TtlSeconds.Value) : null;
    await coordinator.SetAsync(key, request.Value, ttl, cancellationToken);

    return Results.Accepted($"/cache/{Uri.EscapeDataString(key)}", new { key, replicated = true });
});

app.MapGet("/cache/{key}", async (string key, DistributedCacheCoordinator coordinator, CancellationToken cancellationToken) =>
{
    var result = await coordinator.GetAsync(key, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapDelete("/cache/{key}", async (string key, DistributedCacheCoordinator coordinator, CancellationToken cancellationToken) =>
{
    await coordinator.DeleteAsync(key, cancellationToken);
    return Results.NoContent();
});

app.MapPut("/internal/cache/{key}", async (string key, CacheEntryRequest request, ILocalCacheStore localCacheStore, CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Value))
    {
        return Results.BadRequest(new { message = "Value is required." });
    }

    TimeSpan? ttl = request.TtlSeconds is > 0 ? TimeSpan.FromSeconds(request.TtlSeconds.Value) : null;
    await localCacheStore.SetAsync(key, request.Value, ttl, cancellationToken);

    return Results.NoContent();
});

app.MapGet("/internal/cache/{key}", async (string key, ILocalCacheStore localCacheStore, CancellationToken cancellationToken) =>
{
    var item = await localCacheStore.GetAsync(key, cancellationToken);
    return item is null ? Results.NotFound() : Results.Ok(item);
});

app.MapDelete("/internal/cache/{key}", async (string key, ILocalCacheStore localCacheStore, CancellationToken cancellationToken) =>
{
    await localCacheStore.DeleteAsync(key, cancellationToken);
    return Results.NoContent();
});

app.Run();
