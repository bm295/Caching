# Caching Framework

A reusable .NET caching library designed to be packaged, licensed, and extended as a commercial caching framework.

## What is included

- `ICacheClient`: a small application-facing abstraction for `Set`, `Get`, `GetOrCreate`, and `Remove` workflows.
- `MemoryCacheClient`: a production-ready in-process provider backed by `Microsoft.Extensions.Caching.Memory`.
- Region-aware keys for tenant, module, or bounded-context separation.
- Per-entry TTL support with cache hit metadata.
- Rendezvous-hash placement primitives for distributed cache providers.
- Dependency injection registration through `AddCachingFramework`.
- Tests covering cache-aside behavior, regions, and deterministic distributed placement.

## Install from source

```bash
dotnet restore
dotnet build Caching.sln
```

## Register the framework

```csharp
using Caching.Framework.DependencyInjection;
using Caching.Framework.Distributed;

builder.Services.AddCachingFramework(options =>
{
    options.LocalNodeId = "node-a";
    options.ReplicationFactor = 2;
    options.Nodes =
    [
        new CacheNode("node-a", new Uri("https://cache-a.internal")),
        new CacheNode("node-b", new Uri("https://cache-b.internal"))
    ];
});
```

## Use cache-aside in application code

```csharp
using Caching.Framework.Abstractions;

public sealed class CatalogService(ICacheClient cache)
{
    public Task<Product> GetProductAsync(string sku, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            $"product:{sku}",
            token => LoadProductAsync(sku, token),
            new CacheEntryOptions
            {
                Region = "catalog",
                TimeToLive = TimeSpan.FromMinutes(10)
            },
            cancellationToken);
}
```

## Package for distribution

```bash
dotnet pack src/Caching.Framework/Caching.Framework.csproj -c Release
```

The project includes NuGet metadata so the generated package can be published to a private feed or marketplace after you add your final license, branding, documentation site, and support policy.

## Roadmap for commercial hardening

- Add Redis, SQL, and cloud cache provider packages that implement `ICacheClient`.
- Add encryption, compression, stampede protection, metrics, and health checks.
- Add signed packages and source-link metadata.
- Add benchmarks and compatibility tests for supported .NET versions.
