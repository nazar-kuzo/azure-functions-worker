using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class AzureTableCacheDependencyInjectionExtensions
{
    public static FunctionsApplicationBuilder AddAzureTableCache(
        this FunctionsApplicationBuilder worker,
        Action<AzureTableCacheOptions>? configureCacheOptions = null)
    {
        worker.Services
            .AddSingleton<IDistributedCache, AzureTableCache>()
            .Configure<AzureTableCacheOptions>(cacheOptions =>
            {
                worker.Configuration.GetSection("AzureTableCache").Bind(cacheOptions);

                configureCacheOptions?.Invoke(cacheOptions);

                cacheOptions.ApplicationName ??= "Default";
                cacheOptions.ConnectionString ??= worker.Configuration.GetValue<string>("AzureWebJobsStorage")!;
            });

        return worker;
    }
}
