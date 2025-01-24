using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class AzureTableCacheDependencyInjectionExtensions
{

    /// <summary>
    /// Adds Azure Table Storage as a distributed cache.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configureCacheOptions">Optional post configure options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAzureTableCache(
        this IServiceCollection services,
        Action<AzureTableCacheOptions>? configureCacheOptions = null)
    {
        return services.AddAzureTableCacheInternal(configuration: null, configureCacheOptions);
    }

    /// <summary>
    /// Adds Azure Table Storage as a distributed cache.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration to bind <see cref="AzureTableCacheOptions"/></param>
    /// <param name="configureCacheOptions">Optional post configure options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAzureTableCache(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AzureTableCacheOptions>? configureCacheOptions = null)
    {
        return services.AddAzureTableCacheInternal(configuration, configureCacheOptions);
    }

    private static IServiceCollection AddAzureTableCacheInternal(
        this IServiceCollection services,
        IConfiguration? configuration,
        Action<AzureTableCacheOptions>? configureCacheOptions = null)
    {
        services
            .AddSingleton<IDistributedCache, AzureTableCache>()
            .Configure<AzureTableCacheOptions>(cacheOptions =>
            {
                configuration?.Bind(cacheOptions);

                configureCacheOptions?.Invoke(cacheOptions);

                cacheOptions.ApplicationName ??= "Default";
                cacheOptions.ConnectionString ??= configuration?.GetValue<string>("AzureWebJobsStorage")!;
            });

        return services;
    }
}
