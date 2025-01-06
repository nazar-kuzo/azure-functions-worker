using AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights;

public static class WorkerExtensions
{
    public static FunctionsApplicationBuilder ConfigureStandaloneFunctionsApplicationInsights(
        this FunctionsApplicationBuilder builder)
    {
        // configure default worker application insights
        builder.Services.ConfigureFunctionsApplicationInsights();

        // replace built-in module with standalone one
        if (GetFunctionTelemetryModuleDescriptor() is { } descriptor)
        {
            builder.Services.Remove(descriptor);
        }

        builder.Services.AddTransient<IStartupFilter, StartupFilter>();
        builder.Services.AddSingleton<TelemetryClientAccessor>();
        builder.Services.AddSingleton<FunctionActivityCoordinator>();

        builder.Services.AddSingleton<ITelemetryModule, StandaloneFunctionsTelemetryModule>();
        builder.Services.AddApplicationInsightsTelemetryProcessor<FunctionTelemetryProcessor>();

        builder.Logging.AddConfiguration(builder.Configuration.GetSection("Logging"));

        // remove default logger filter rules
        builder.Logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            var applicationInsightsLoggerProviderName = typeof(ApplicationInsightsLoggerProvider).FullName;

            if (options.Rules is List<LoggerFilterRule> rules)
            {
                // remove all default rules set by SDK
                rules.RemoveAll(rule => rule.ProviderName == applicationInsightsLoggerProviderName);
            }
        });

        return builder;

        ServiceDescriptor? GetFunctionTelemetryModuleDescriptor()
        {
            return builder.Services.FirstOrDefault(service =>
                service.ServiceType == typeof(ITelemetryModule) &&
                service.ImplementationType!.Name == "FunctionsTelemetryModule");
        }
    }
}
