using AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.WorkerService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.ApplicationInsights;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights;

public static class WorkerExtensions
{
    public static FunctionsApplicationBuilder ConfigureFunctionsWebApplicationWithStandaloneApplicationInsights(
        this FunctionsApplicationBuilder worker,
        Action<ApplicationInsightsServiceOptions> configureServiceOptions)
    {
        // this middleware should be added before aspnet core middleware from "ConfigureFunctionsWebApplication"
        worker.UseMiddleware<FunctionApplicationInsightsMiddleware>();

        worker.ConfigureFunctionsWebApplication();

        // configure default worker application insights
        worker.Services.ConfigureFunctionsApplicationInsights();

        // replace built-in module with standalone one
        if (GetFunctionTelemetryModuleDescriptor() is { } descriptor)
        {
            worker.Services.Remove(descriptor);
        }

        worker.Services.AddTransient<IStartupFilter, StartupFilter>();
        worker.Services.AddSingleton<TelemetryClientAccessor>();
        worker.Services.AddSingleton<FunctionActivityCoordinator>();

        worker.Services.AddSingleton<ITelemetryModule, StandaloneFunctionsTelemetryModule>();
        worker.Services.AddApplicationInsightsTelemetryProcessor<FunctionTelemetryProcessor>();

        worker.Logging.AddConfiguration(worker.Configuration.GetSection("Logging"));

        // remove default logger filter rules
        worker.Logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            var applicationInsightsLoggerProviderName = typeof(ApplicationInsightsLoggerProvider).FullName;

            if (options.Rules is List<LoggerFilterRule> rules)
            {
                // remove all default rules set by SDK
                rules.RemoveAll(rule => rule.ProviderName == applicationInsightsLoggerProviderName);
            }
        });

        worker.Services.AddApplicationInsightsTelemetryWorkerService(appInsightsOptions => configureServiceOptions?.Invoke(appInsightsOptions));

        return worker;

        ServiceDescriptor? GetFunctionTelemetryModuleDescriptor()
        {
            return worker.Services.FirstOrDefault(service =>
                service.ServiceType == typeof(ITelemetryModule) &&
                service.ImplementationType!.Name == "FunctionsTelemetryModule");
        }
    }
}
