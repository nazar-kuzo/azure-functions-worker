using DurableTask.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

public static class HostApplicationExtensions
{
    /// <summary>
    /// Configures the DurableTask clients for the Azure Functions Worker.
    /// </summary>
    /// <param name="worker">Functions application builder</param>
    /// <param name="sectionName">Configuration section name</param>
    /// <param name="configureClientOptions">DurableTaskClientOptions configurator</param>
    /// <returns>FunctionsApplicationBuilder</returns>
    public static IHostApplicationBuilder ConfigureDurableTaskClient(
        this IHostApplicationBuilder worker,
        string sectionName = "DurableTask",
        Action<DurableTaskClientOptions>? configureClientOptions = null)
    {
        worker.Services.AddSingleton<SystemTextJsonDataConverter>();

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

            configureClientOptions?.Invoke(options);
        }
    }
}
