# Market Readiness TODO

This checklist breaks market-readiness work into very small, concrete tasks. Prefer turning each unchecked item into one issue or one tiny pull request. Items that mention a class, interface, record, file, or test should use the exact name shown unless a later design review changes it.

## Product identity and package metadata

- [x] Create `src/Caching.Framework/Branding/ProductBrand.cs`.
- [x] Add a `public const string Name = "Caching.Framework"` field to `ProductBrand`.
- [x] Add a `public const string DisplayName = "Caching Framework"` field to `ProductBrand`.
- [x] Add a `public const string Tagline` field to `ProductBrand`.
- [x] Add a `public const string WebsiteUrl` field to `ProductBrand`.
- [x] Add a `public const string DocumentationUrl` field to `ProductBrand`.
- [x] Add a `public const string SupportUrl` field to `ProductBrand`.
- [ ] Add a unit test named `ProductBrand_HasNonEmptyDisplayName`.
- [ ] Add a unit test named `ProductBrand_HasNonEmptyTagline`.
- [ ] Update `src/Caching.Framework/Caching.Framework.csproj` with a final `PackageProjectUrl` value.
- [ ] Update `src/Caching.Framework/Caching.Framework.csproj` with a final `RepositoryUrl` value.
- [ ] Update `src/Caching.Framework/Caching.Framework.csproj` with `PackageTags`.
- [ ] Update `src/Caching.Framework/Caching.Framework.csproj` with `PackageIcon`.
- [ ] Create `assets/package-icon.png`.
- [ ] Add `assets/package-icon.png` to the NuGet package.

## Repository structure

- [ ] Create `src/Caching.Framework.Abstractions/Caching.Framework.Abstractions.csproj`.
- [ ] Move `src/Caching.Framework/Abstractions/ICacheClient.cs` to the abstractions project.
- [ ] Move `src/Caching.Framework/Abstractions/CacheItem.cs` to the abstractions project.
- [ ] Move `src/Caching.Framework/Abstractions/CacheEntryOptions.cs` to the abstractions project.
- [ ] Create `src/Caching.Framework.Memory/Caching.Framework.Memory.csproj`.
- [ ] Move `src/Caching.Framework/Memory/MemoryCacheClient.cs` to the memory project.
- [ ] Create `src/Caching.Framework.Distributed/Caching.Framework.Distributed.csproj`.
- [ ] Move `src/Caching.Framework/Distributed/CacheNode.cs` to the distributed project.
- [ ] Move `src/Caching.Framework/Distributed/CachePlacementService.cs` to the distributed project.
- [ ] Move `src/Caching.Framework/Distributed/DistributedCacheOptions.cs` to the distributed project.
- [ ] Move `src/Caching.Framework/Distributed/RendezvousHashRing.cs` to the distributed project.
- [ ] Create `samples/Caching.Framework.SampleApi/Caching.Framework.SampleApi.csproj`.
- [ ] Move demo HTTP endpoints from `src/Caching.Framework/Program.cs` into the sample API project.
- [ ] Create `samples/Caching.Framework.SampleConsole/Caching.Framework.SampleConsole.csproj`.
- [ ] Add `Caching.sln` entries for each new project.
- [ ] Add project references from the sample API to the product library projects.

## Public API classes and interfaces

- [ ] Create `src/Caching.Framework.Abstractions/Keys/CacheKey.cs`.
- [ ] Implement `public sealed record CacheKey(string Value)`.
- [ ] Add validation that `CacheKey.Value` cannot be empty.
- [ ] Add a unit test named `CacheKey_RejectsEmptyValue`.
- [ ] Create `src/Caching.Framework.Abstractions/Keys/CacheRegion.cs`.
- [ ] Implement `public sealed record CacheRegion(string Value)`.
- [ ] Add validation that `CacheRegion.Value` cannot contain `:`.
- [ ] Add a unit test named `CacheRegion_RejectsColon`.
- [ ] Create `src/Caching.Framework.Abstractions/Keys/ICacheKeyBuilder.cs`.
- [ ] Add `string Build(CacheRegion? region, CacheKey key)` to `ICacheKeyBuilder`.
- [ ] Create `src/Caching.Framework/Keys/DefaultCacheKeyBuilder.cs`.
- [ ] Implement `DefaultCacheKeyBuilder.Build` using the existing region/key format.
- [ ] Add a unit test named `DefaultCacheKeyBuilder_BuildsRegionPrefixedKey`.
- [ ] Create `src/Caching.Framework.Abstractions/Serialization/ICacheSerializer.cs`.
- [ ] Add `byte[] Serialize<T>(T value)` to `ICacheSerializer`.
- [ ] Add `T? Deserialize<T>(byte[] bytes)` to `ICacheSerializer`.
- [ ] Create `src/Caching.Framework/Serialization/SystemTextJsonCacheSerializer.cs`.
- [ ] Add a unit test named `SystemTextJsonCacheSerializer_RoundTripsObject`.
- [ ] Create `src/Caching.Framework.Abstractions/Policies/CacheTtlPolicy.cs`.
- [ ] Add `TimeSpan? DefaultTtl` to `CacheTtlPolicy`.
- [ ] Add `Dictionary<string, TimeSpan> RegionTtls` to `CacheTtlPolicy`.
- [ ] Create `src/Caching.Framework.Abstractions/Policies/CacheReadPolicy.cs`.
- [ ] Add `bool AllowStaleOnError` to `CacheReadPolicy`.
- [ ] Create `src/Caching.Framework.Abstractions/Policies/CacheWritePolicy.cs`.
- [ ] Add `bool ReplicateSynchronously` to `CacheWritePolicy`.

## Feature classes

- [ ] Create `src/Caching.Framework/Stampede/ICacheStampedeGuard.cs`.
- [ ] Add `Task<T> RunAsync<T>(string key, Func<CancellationToken, Task<T>> factory, CancellationToken cancellationToken)` to `ICacheStampedeGuard`.
- [ ] Create `src/Caching.Framework/Stampede/InMemoryCacheStampedeGuard.cs`.
- [ ] Add a unit test named `InMemoryCacheStampedeGuard_CallsFactoryOnceForSameKey`.
- [ ] Create `src/Caching.Framework/Refresh/IBackgroundCacheRefresher.cs`.
- [ ] Add `Task ScheduleRefreshAsync(string key, CancellationToken cancellationToken)` to `IBackgroundCacheRefresher`.
- [ ] Create `src/Caching.Framework/Refresh/NoOpBackgroundCacheRefresher.cs`.
- [ ] Create `src/Caching.Framework/Compression/ICacheCompressor.cs`.
- [ ] Add `byte[] Compress(byte[] bytes)` to `ICacheCompressor`.
- [ ] Add `byte[] Decompress(byte[] bytes)` to `ICacheCompressor`.
- [ ] Create `src/Caching.Framework/Compression/GzipCacheCompressor.cs`.
- [ ] Add a unit test named `GzipCacheCompressor_DecompressesCompressedPayload`.
- [ ] Create `src/Caching.Framework/Encryption/ICacheEncryptor.cs`.
- [ ] Add `byte[] Encrypt(byte[] bytes)` to `ICacheEncryptor`.
- [ ] Add `byte[] Decrypt(byte[] bytes)` to `ICacheEncryptor`.
- [ ] Create `src/Caching.Framework/Encryption/NoOpCacheEncryptor.cs`.
- [ ] Create `src/Caching.Framework/Tags/CacheTag.cs`.
- [ ] Implement `public sealed record CacheTag(string Value)`.
- [ ] Create `src/Caching.Framework/Tags/ICacheTagIndex.cs`.
- [ ] Add `Task AddAsync(CacheTag tag, CacheKey key, CancellationToken cancellationToken)` to `ICacheTagIndex`.
- [ ] Add `Task<IReadOnlyCollection<CacheKey>> GetKeysAsync(CacheTag tag, CancellationToken cancellationToken)` to `ICacheTagIndex`.
- [ ] Create `src/Caching.Framework/Tags/InMemoryCacheTagIndex.cs`.

## Provider packages

- [ ] Create `src/Caching.Framework.Providers/Caching.Framework.Providers.csproj`.
- [ ] Create `src/Caching.Framework.Providers/ICacheProvider.cs`.
- [ ] Add `string ProviderName { get; }` to `ICacheProvider`.
- [ ] Add `CacheProviderCapabilities Capabilities { get; }` to `ICacheProvider`.
- [ ] Create `src/Caching.Framework.Providers/CacheProviderCapabilities.cs`.
- [ ] Add `bool SupportsDistributedStorage` to `CacheProviderCapabilities`.
- [ ] Add `bool SupportsTags` to `CacheProviderCapabilities`.
- [ ] Add `bool SupportsAtomicCompareAndSet` to `CacheProviderCapabilities`.
- [ ] Create `src/Caching.Framework.Redis/Caching.Framework.Redis.csproj`.
- [ ] Create `src/Caching.Framework.Redis/RedisCacheClient.cs`.
- [ ] Implement `ICacheClient` in `RedisCacheClient`.
- [ ] Create `src/Caching.Framework.Redis/RedisCacheOptions.cs`.
- [ ] Add `string ConnectionString` to `RedisCacheOptions`.
- [ ] Create `src/Caching.Framework.Redis/RedisCacheServiceCollectionExtensions.cs`.
- [ ] Add `AddRedisCachingFramework` extension method.
- [ ] Create `tests/Caching.Framework.Redis.Tests/RedisCacheClientTests.cs`.

## Security hardening

- [ ] Create `src/Caching.Framework/Security/CacheApiAuthOptions.cs`.
- [ ] Add `bool RequireAuthentication` to `CacheApiAuthOptions`.
- [ ] Add `string? SharedSecret` to `CacheApiAuthOptions`.
- [ ] Create `src/Caching.Framework/Security/PeerAuthenticationHandler.cs`.
- [ ] Add shared-secret validation to `PeerAuthenticationHandler`.
- [ ] Add a unit test named `PeerAuthenticationHandler_RejectsMissingSecret`.
- [ ] Create `src/Caching.Framework/Security/CacheKeyValidationOptions.cs`.
- [ ] Add `int MaxKeyLength` to `CacheKeyValidationOptions`.
- [ ] Add `string[] ForbiddenKeyPrefixes` to `CacheKeyValidationOptions`.
- [ ] Create `src/Caching.Framework/Security/CacheValueValidationOptions.cs`.
- [ ] Add `int MaxValueBytes` to `CacheValueValidationOptions`.
- [ ] Add key length validation to the HTTP PUT endpoint.
- [ ] Add value size validation to the HTTP PUT endpoint.
- [ ] Add authorization to the HTTP DELETE endpoint.
- [ ] Add rate limiting to public cache endpoints.

## Observability classes

- [ ] Create `src/Caching.Framework/Observability/CacheMetricNames.cs`.
- [ ] Add `public const string Hits = "cache.hits"` to `CacheMetricNames`.
- [ ] Add `public const string Misses = "cache.misses"` to `CacheMetricNames`.
- [ ] Add `public const string Writes = "cache.writes"` to `CacheMetricNames`.
- [ ] Add `public const string Deletes = "cache.deletes"` to `CacheMetricNames`.
- [ ] Create `src/Caching.Framework/Observability/ICacheMetrics.cs`.
- [ ] Add `void RecordHit(string region)` to `ICacheMetrics`.
- [ ] Add `void RecordMiss(string region)` to `ICacheMetrics`.
- [ ] Add `void RecordWrite(string region)` to `ICacheMetrics`.
- [ ] Add `void RecordDelete(string region)` to `ICacheMetrics`.
- [ ] Create `src/Caching.Framework/Observability/OpenTelemetryCacheMetrics.cs`.
- [ ] Create `src/Caching.Framework/Health/LocalCacheHealthCheck.cs`.
- [ ] Create `src/Caching.Framework/Health/RedisCacheHealthCheck.cs`.
- [ ] Create `src/Caching.Framework/Health/PeerNodeHealthCheck.cs`.

## Reliability classes

- [ ] Create `src/Caching.Framework/Reliability/CacheRetryOptions.cs`.
- [ ] Add `int MaxRetryAttempts` to `CacheRetryOptions`.
- [ ] Add `TimeSpan Delay` to `CacheRetryOptions`.
- [ ] Create `src/Caching.Framework/Reliability/CacheTimeoutOptions.cs`.
- [ ] Add `TimeSpan OperationTimeout` to `CacheTimeoutOptions`.
- [ ] Create `src/Caching.Framework/Reliability/CacheCircuitBreakerOptions.cs`.
- [ ] Add `int FailureThreshold` to `CacheCircuitBreakerOptions`.
- [ ] Add `TimeSpan BreakDuration` to `CacheCircuitBreakerOptions`.
- [ ] Create `src/Caching.Framework/Reliability/ICacheFallbackPolicy.cs`.
- [ ] Add `Task<T?> OnReadFailureAsync<T>(string key, Exception exception, CancellationToken cancellationToken)` to `ICacheFallbackPolicy`.
- [ ] Create `src/Caching.Framework/Reliability/NullCacheFallbackPolicy.cs`.

## Documentation files

- [ ] Create `docs/index.md`.
- [ ] Create `docs/quickstart.md`.
- [ ] Create `docs/installation.md`.
- [ ] Create `docs/configuration.md`.
- [ ] Create `docs/cache-aside.md`.
- [ ] Create `docs/distributed-caching.md`.
- [ ] Create `docs/providers.md`.
- [ ] Create `docs/security.md`.
- [ ] Create `docs/observability.md`.
- [ ] Create `docs/operations.md`.
- [ ] Create `docs/troubleshooting.md`.
- [ ] Create `docs/migration.md`.
- [ ] Create `docs/comparison.md`.
- [ ] Create `docs/pricing.md`.
- [ ] Create `docs/support.md`.
- [ ] Create `CHANGELOG.md`.
- [ ] Create `SECURITY.md`.
- [ ] Create `SUPPORT.md`.
- [ ] Create `LICENSE`.

## Testing and benchmarks

- [ ] Create `tests/Caching.Framework.Abstractions.Tests/CacheKeyTests.cs`.
- [ ] Create `tests/Caching.Framework.Abstractions.Tests/CacheRegionTests.cs`.
- [ ] Create `tests/Caching.Framework.Tests/DefaultCacheKeyBuilderTests.cs`.
- [ ] Create `tests/Caching.Framework.Tests/SystemTextJsonCacheSerializerTests.cs`.
- [ ] Create `tests/Caching.Framework.Tests/InMemoryCacheStampedeGuardTests.cs`.
- [ ] Create `tests/Caching.Framework.Tests/GzipCacheCompressorTests.cs`.
- [ ] Create `tests/Caching.Framework.Tests/InMemoryCacheTagIndexTests.cs`.
- [ ] Create `tests/Caching.Framework.Tests/CacheMetricNamesTests.cs`.
- [ ] Create `tests/Caching.Framework.Tests/CacheRetryOptionsTests.cs`.
- [ ] Create `benchmarks/Caching.Framework.Benchmarks/Caching.Framework.Benchmarks.csproj`.
- [ ] Create `benchmarks/Caching.Framework.Benchmarks/MemoryCacheClientBenchmarks.cs`.
- [ ] Add a `GetAsync_Hit` benchmark method.
- [ ] Add a `GetAsync_Miss` benchmark method.
- [ ] Add a `SetAsync_SmallValue` benchmark method.
- [ ] Add a `GetOrCreateAsync_Hit` benchmark method.
- [ ] Add a `GetOrCreateAsync_Miss` benchmark method.

## CI and release automation

- [ ] Create `.github/workflows/ci.yml`.
- [ ] Add a `dotnet restore Caching.sln` step to CI.
- [ ] Add a `dotnet build Caching.sln -c Release --no-restore` step to CI.
- [ ] Add a `dotnet test Caching.sln -c Release --no-build` step to CI.
- [ ] Add a `dotnet pack src/Caching.Framework/Caching.Framework.csproj -c Release --no-build` step to CI.
- [ ] Create `.github/workflows/release.yml`.
- [ ] Add a manual `workflow_dispatch` trigger to release CI.
- [ ] Add a package version input to release CI.
- [ ] Add a signed package creation step to release CI.
- [ ] Add a NuGet publish dry-run step to release CI.
- [ ] Create `RELEASE_CHECKLIST.md`.
- [ ] Create `ROLLBACK_CHECKLIST.md`.

## Sales, support, and launch assets

- [ ] Create `sales/product-one-pager.md`.
- [ ] Create `sales/demo-script.md`.
- [ ] Create `sales/pilot-success-checklist.md`.
- [ ] Create `sales/customer-onboarding-checklist.md`.
- [ ] Create `sales/pricing-notes.md`.
- [ ] Create `sales/competitor-comparison.md`.
- [ ] Create `support/support-intake-template.md`.
- [ ] Create `support/incident-template.md`.
- [ ] Create `support/faq.md`.
- [ ] Create `support/troubleshooting-intake-questions.md`.
