# Packaging and distributing Caching.Framework

This guide explains how to build a NuGet package from this repository and share it with another team, customer, or partner.

## Prerequisites

- Install the .NET 8 SDK or later.
- Run commands from the repository root.
- Decide whether you will distribute the package through a private NuGet feed, a local folder/feed, or a direct `.nupkg` file transfer.

## 1. Restore, build, and test

```bash
dotnet restore Caching.sln
dotnet build Caching.sln -c Release --no-restore
dotnet test Caching.sln -c Release --no-build
```

## 2. Create the NuGet package

```bash
dotnet pack src/Caching.Framework/Caching.Framework.csproj -c Release -o ./artifacts/packages
```

The output will be a package similar to:

```text
artifacts/packages/Caching.Framework.1.0.0.nupkg
```

## 3. Choose a distribution option

### Option A: Private NuGet feed

Use this when you want versioning, access control, and an easy update path.

Examples include GitHub Packages, Azure Artifacts, ProGet, Nexus, Artifactory, or a private NuGet.org organization feed.

```bash
dotnet nuget push ./artifacts/packages/Caching.Framework.1.0.0.nupkg \
  --source <PRIVATE_FEED_URL> \
  --api-key <API_KEY>
```

The consuming party adds your feed and installs the package:

```bash
dotnet nuget add source <PRIVATE_FEED_URL> --name YourCompanyCacheFeed
dotnet add package Caching.Framework --version 1.0.0
```

### Option B: Local folder feed

Use this for pilots, offline delivery, or customer environments without hosted package infrastructure.

Create a folder that contains the `.nupkg` file and give the folder to the consuming party. They can add it as a NuGet source:

```bash
dotnet nuget add source /path/to/packages --name LocalCachingFrameworkFeed
dotnet add package Caching.Framework --version 1.0.0
```

### Option C: Direct `.nupkg` transfer

Use this only for a one-off evaluation. Send the `.nupkg` file plus this repository's README and license terms. The recipient can install it by first adding the containing folder as a local feed.

## 4. Version future releases

Set the package version at pack time:

```bash
dotnet pack src/Caching.Framework/Caching.Framework.csproj \
  -c Release \
  -o ./artifacts/packages \
  -p:PackageVersion=1.1.0
```

Recommended versioning pattern:

- Patch, such as `1.0.1`, for bug fixes.
- Minor, such as `1.1.0`, for backward-compatible features.
- Major, such as `2.0.0`, for breaking changes.

## 5. What to give external parties

For a professional delivery, provide:

- The `.nupkg` file or private feed URL.
- Package version and release notes.
- README usage examples.
- License and commercial terms.
- Supported .NET versions.
- Contact/support process.

## 6. Consumer usage example

After installation, consuming applications can register the framework with dependency injection:

```csharp
using Caching.Framework.DependencyInjection;

builder.Services.AddCachingFramework();
```

Then inject `ICacheClient` where caching is needed:

```csharp
using Caching.Framework.Abstractions;

public sealed class ProductService(ICacheClient cache)
{
    public Task<Product> GetAsync(string sku, CancellationToken cancellationToken) =>
        cache.GetOrCreateAsync(
            $"product:{sku}",
            token => LoadFromDatabaseAsync(sku, token),
            CacheEntryOptions.WithTtl(TimeSpan.FromMinutes(10)),
            cancellationToken);
}
```
