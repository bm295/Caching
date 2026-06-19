using Caching.Framework.Abstractions;
using Caching.Framework.Distributed;
using Caching.Framework.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace Caching.Framework.DependencyInjection;

public static class CachingFrameworkServiceCollectionExtensions
{
    public static IServiceCollection AddCachingFramework(this IServiceCollection services, Action<DistributedCacheOptions>? configureDistributed = null)
    {
        services.AddMemoryCache();
        services.AddSingleton<ICacheClient, MemoryCacheClient>();
        services.AddSingleton<CachePlacementService>();
        if (configureDistributed is not null)
        {
            services.Configure(configureDistributed);
        }
        else
        {
            services.Configure<DistributedCacheOptions>(_ => { });
        }

        return services;
    }
}
