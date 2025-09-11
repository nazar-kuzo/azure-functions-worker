using AzureFunctions.Worker.Extensions.DurableTask.Client.Internal;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Azure.Functions.Worker.Extensions.DurableTask;
using Microsoft.Extensions.Configuration;

namespace Microsoft.Extensions.DependencyInjection;

public static class WorkerExtensions
{
    /// <summary>
    /// Configures the DurableTask clients for the Azure Functions Worker.
    /// </summary>
    /// <param name="worker">Functions application builder</param>
    /// <param name="sectionName">Configuration section name</param>
    /// <returns>FunctionsApplicationBuilder</returns>
    public static FunctionsApplicationBuilder ConfigureDurableTaskClient(
        this FunctionsApplicationBuilder worker,
        string sectionName = "DurableTask")
    {
        worker.Services
            .AddSingleton<FunctionMethodInfoLocator>()
            .AddSingleton<SystemTextJsonDataConverter>();

        var rootConfiguration = worker.Configuration.GetSection(sectionName);

        // when no configuration or single client configuration provided, otherwise bind multiple named configurations
        if (!rootConfiguration.Exists() ||
            rootConfiguration.GetSection(nameof(DurableTaskClientOptions.ConnectionString)).Exists() ||
            rootConfiguration.GetSection(nameof(DurableTaskClientOptions.TaskHubName)).Exists())
        {
            worker.Services
                .AddSingleton<DurableTaskClient>()
                .Configure<DurableTaskClientOptions>(options => ConfigureClientOptions(options, rootConfiguration))
                .AddOptionsWithValidateOnStart<DurableTaskClientOptions, DurableTaskClientOptionsValidator>();
        }
        else
        {
            foreach (var configSection in rootConfiguration.GetSection(sectionName).GetChildren())
            {
                worker.Services
                    .AddKeyedSingleton<DurableTaskClient>(configSection.Key)
                    .Configure<DurableTaskClientOptions>(configSection.Key, options => ConfigureClientOptions(options, configSection))
                    .AddOptionsWithValidateOnStart<DurableTaskClientOptions, DurableTaskClientOptionsValidator>();
            }
        }

        return worker;

        void ConfigureClientOptions(DurableTaskClientOptions options, IConfigurationSection section)
        {
            section.Bind(options);

            options.ConnectionString ??= worker.Configuration.GetValue<string>("AzureWebJobsStorage")!;
            options.TaskHubName ??= "TestHubName";
        }
    }
}
