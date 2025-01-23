using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class AzureTableCacheDependencyInjectionExtensions
{
    /// <summary>
    /// Adds Azure Table Storage as a distributed cache.
    /// </summary>
    /// <param name="worker">Functions application builder</param>
    /// <param name="configureCacheOptions">Optional post configure options bound from `AzureTableCache` config section</param>
    /// <returns>Functions application builder for chaining</returns>
    public static FunctionsApplicationBuilder AddAzureTableCache(
        this FunctionsApplicationBuilder worker,
        Action<AzureTableCacheOptions>? configureCacheOptions = null)
    {
        worker.Services.AddAzureTableCache(worker.Configuration, configureCacheOptions);

        return worker;
    }

    /// <summary>
    /// Adds Azure Table Storage as a distributed cache.
    /// </summary>
    /// <param name="services">Service collection</param>
    /// <param name="configuration">Configuration to bind `AzureTableCache` section</param>
    /// <param name="configureCacheOptions">Optional post configure options</param>
    /// <returns>Service collection for chaining</returns>
    public static IServiceCollection AddAzureTableCache(
        this IServiceCollection services,
        IConfiguration configuration,
        Action<AzureTableCacheOptions>? configureCacheOptions = null)
    {
        services
            .AddSingleton<IDistributedCache, AzureTableCache>()
            .Configure<AzureTableCacheOptions>(cacheOptions =>
            {
                configuration.GetSection("AzureTableCache").Bind(cacheOptions);

                configureCacheOptions?.Invoke(cacheOptions);

                cacheOptions.ApplicationName ??= "Default";
                cacheOptions.ConnectionString ??= configuration.GetValue<string>("AzureWebJobsStorage")!;
            });

        return services;
    }
}
