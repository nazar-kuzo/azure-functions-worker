using System.Diagnostics;
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
    /// <summary>
    /// Configures standalone Application Insights for Azure Functions Worker application.
    /// With this approach, host telemetry is ignored and worker gets full control over request telemetry.
    /// </summary>
    /// <param name="worker">Functions application builder</param>
    /// <param name="enableHttpRequestMapping">Whether HTTP triggered functions should map their request info and response code</param>
    /// <param name="configureServiceOptions">Action to configure <see cref="ApplicationInsightsServiceOptions"/></param>
    /// <returns>Functions application builder for chaining methods</returns>
    /// <exception cref="InvalidOperationException">Is thrown when method is not called before &quot;ConfigureFunctionsWebApplication&quot;</exception>
    public static FunctionsApplicationBuilder ConfigureStandaloneApplicationInsights(
        this FunctionsApplicationBuilder worker,
        bool enableHttpRequestMapping = true,
        Action<ApplicationInsightsServiceOptions>? configureServiceOptions = null)
    {
        if (worker.Services.Any(descriptor => descriptor.ImplementationType?.FullName == "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.DefaultHttpCoordinator"))
        {
            throw new InvalidOperationException($"This method should be called before the \"ConfigureFunctionsWebApplication\" method");
        }

        worker.Services.AddApplicationInsightsTelemetryWorkerService(appInsightsOptions => configureServiceOptions?.Invoke(appInsightsOptions));

        worker.Services.ConfigureFunctionsApplicationInsights();

        RemoveBuiltInFunctionsTelemetryModule();

        if (enableHttpRequestMapping)
        {
            worker.Services.AddSingleton<HttpRequestActivityCoordinator>();
            worker.Services.AddTransient<IStartupFilter, WorkerStartupFilter>();
            worker.Services.AddSingleton<WorkerHttpRequestMappingMiddleware>();
        }

        worker.Services.AddApplicationInsightsTelemetryProcessor<FunctionTelemetryProcessor>();

        worker.Logging.AddConfiguration(worker.Configuration.GetSection("Logging"));

        worker.RemoveDefaultLoggerFilterRules();
        worker.AddFunctionContextAccessor();

        worker.UseMiddleware<FunctionRequestTelemetryMiddleware>();

        return worker;

        void RemoveBuiltInFunctionsTelemetryModule()
        {
            var telemetryModuleDescriptor = worker.Services.FirstOrDefault(service =>
                service.ServiceType == typeof(ITelemetryModule) &&
                service.ImplementationType!.FullName == "Microsoft.Azure.Functions.Worker.ApplicationInsights.FunctionsTelemetryModule");

            if (telemetryModuleDescriptor is not null)
            {
                worker.Services.Remove(telemetryModuleDescriptor);

                // ensures that function telemetry provider will create host activity
                ActivitySource.AddActivityListener(new()
                {
                    ShouldListenTo = source => source.Name.StartsWith("Microsoft.Azure.Functions.Worker"),
                    Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
                    SampleUsingParentId = (ref ActivityCreationOptions<string> _) => ActivitySamplingResult.AllData,
                });
            }
        }
    }

    /// <summary>
    /// Enables access to the current function execution context within Azure Functions.
    /// </summary>
    /// <param name="worker">Functions application builder</param>
    /// <returns>Functions application builder for chaining methods</returns>
    public static FunctionsApplicationBuilder AddFunctionContextAccessor(this FunctionsApplicationBuilder worker)
    {
        worker.UseMiddleware<FunctionContextAccessorMiddleware>();

        worker.Services.AddSingleton<IFunctionContextAccessor, FunctionContextAccessor>();

        return worker;
    }

    /// <summary>
    /// Fixes out of the box Application Insights behavior that adds a default logging filter
    /// that instructs ILogger to capture only Warning and more severe logs.
    /// </summary>
    /// <param name="worker">Functions application builder</param>
    /// <returns>Functions application builder for chaining methods</returns>
    public static FunctionsApplicationBuilder RemoveDefaultLoggerFilterRules(this FunctionsApplicationBuilder worker)
    {
        worker.Logging.Services.Configure<LoggerFilterOptions>(options =>
        {
            var applicationInsightsLoggerProviderName = typeof(ApplicationInsightsLoggerProvider).FullName;

            if (options.Rules is List<LoggerFilterRule> rules)
            {
                // remove all default rules set by SDK
                rules.RemoveAll(rule => rule.ProviderName == applicationInsightsLoggerProviderName);
            }
        });

        return worker;
    }
}
