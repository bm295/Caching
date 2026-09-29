# BusinessAwareCache

In-process, policy-based cache library for .NET 9. Register policies by result type:

```csharp
using BusinessAwareCache;

services.AddBusinessAwareCache(policies =>
    policies.Add<Product>(new CachePolicy(TimeSpan.FromMinutes(5), "v2", ["products"])));
```

Use `ICacheService` from dependency injection:

```csharp
using BusinessAwareCache;

Product? product = await cache.GetOrCreateAsync<Product>(
    productId, token => repository.FindAsync(productId, token), cancellationToken: cancellationToken);
cache.Remove<Product>(productId);
cache.RemoveTag("products");
```

Pass a `CachePolicy` to a call to override the type policy, including per-item tags. The default key builder combines the assembly-qualified result type, policy version, and identifier with length prefixes. Choose identifiers that uniquely represent the business query, including tenant or locale where relevant; increment `Version` after a data shape change. Applications can replace `ICacheKeyBuilder` in DI. `Remove<T>` must use the same exact result type and version as the lookup.

TTL must be positive. A null result and a factory exception are never cached. Concurrent calls for the same key share one load within this service instance; waiting callers can cancel independently. A failed or null load permits the next caller to retry. Removing a tag evicts entries currently indexed under it and prevents a load already running for that tag from publishing. Key removal only evicts entries already published. Tag removal, coordination, and cache storage are local to one process; there is no cross-instance consistency. Keep tag cardinality bounded. Metrics from the `BusinessAwareCache` meter record hits, misses and load duration without key or tag attributes.
