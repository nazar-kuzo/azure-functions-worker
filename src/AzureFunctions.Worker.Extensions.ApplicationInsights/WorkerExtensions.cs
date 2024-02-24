using AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights;

public static class WorkerExtensions
{
    public static IServiceCollection ConfigureStandaloneFunctionsApplicationInsights(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // configure default worker application insights
        services.ConfigureFunctionsApplicationInsights();

        // replace built-in module with standalone one
        if (GetFunctionTelemetryModuleDescriptor(services) is { } descriptor)
        {
            services.Remove(descriptor);
        }

        services.AddTransient<IStartupFilter, StartupFilter>();
        services.AddSingleton<TelemetryClientAccessor>();
        services.AddSingleton<FunctionActivityCoordinator>();

        services.AddSingleton<ITelemetryModule, StandaloneFunctionsTelemetryModule>();
        services.AddApplicationInsightsTelemetryProcessor<FunctionTelemetryProcessor>();

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.AddConfiguration(configuration.GetSection("Logging"));

            // remove default logger filter rules
            loggingBuilder.Services.Configure<LoggerFilterOptions>(options =>
            {
                if (options.Rules is List<LoggerFilterRule> rules)
                {
                    // remove all default rules set by SDK
                    rules.RemoveAll(rule => rule.ProviderName == typeof(ApplicationInsightsLoggerProvider).FullName);
                }
            });
        });

        return services;
    }

    private static ServiceDescriptor? GetFunctionTelemetryModuleDescriptor(IServiceCollection services)
    {
        return services.FirstOrDefault(service =>
            service.ServiceType == typeof(ITelemetryModule) &&
            service.ImplementationType!.Name == "FunctionsTelemetryModule");
    }
}
