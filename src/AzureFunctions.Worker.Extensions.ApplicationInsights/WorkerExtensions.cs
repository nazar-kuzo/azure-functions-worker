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
    public static FunctionsApplicationBuilder ConfigureStandaloneApplicationInsights(
        this FunctionsApplicationBuilder worker,
        Action<ApplicationInsightsServiceOptions>? configureServiceOptions = null)
    {
        if (worker.Services.Any(descriptor => descriptor.ImplementationType?.FullName == "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.DefaultHttpCoordinator"))
        {
            throw new InvalidOperationException($"This method should be called before the \"ConfigureFunctionsWebApplication\" method");
        }

        worker.Services.AddApplicationInsightsTelemetryWorkerService(appInsightsOptions => configureServiceOptions?.Invoke(appInsightsOptions));

        worker.Services.ConfigureFunctionsApplicationInsights();

        RemoveBuiltInFunctionsTelemetryModule();

        worker.Services.AddTransient<IStartupFilter, StartupFilter>();
        worker.Services.AddSingleton<HttpActivityCoordinator>();

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

        worker.UseMiddleware<FunctionApplicationInsightsMiddleware>();

        return worker;

        void RemoveBuiltInFunctionsTelemetryModule()
        {
            var telemetryModuleDescriptor = worker.Services.FirstOrDefault(service =>
                service.ServiceType == typeof(ITelemetryModule) &&
                service.ImplementationType!.FullName == "Microsoft.Azure.Functions.Worker.ApplicationInsights.FunctionsTelemetryModule");

            if (telemetryModuleDescriptor is not null)
            {
                worker.Services.Remove(telemetryModuleDescriptor);
            }
        }
    }
}
