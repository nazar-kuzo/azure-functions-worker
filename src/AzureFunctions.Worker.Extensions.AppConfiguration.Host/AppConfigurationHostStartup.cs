using Azure.Identity;
using AzureFunctions.Worker.Extensions.AppConfiguration.Host;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

[assembly: WebJobsStartup(typeof(AppConfigurationHostStartup))]

namespace AzureFunctions.Worker.Extensions.AppConfiguration.Host;

public class AppConfigurationHostStartup : IWebJobsStartup, IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
        if (TryGetAppConfigurationEndpoint() is string appConfigEndpoint)
        {
            // TODO: add option for credentials local cache to improve performance
            var credentials = new DefaultAzureCredential(new DefaultAzureCredentialOptions
            {
                ExcludeEnvironmentCredential = true,
                ExcludeWorkloadIdentityCredential = true,
            });

            builder.ConfigurationBuilder.AddAzureAppConfiguration(appConfigOptions =>
            {
                // TODO: add configuration filtering by tags
                appConfigOptions.Connect(new Uri(appConfigEndpoint), credentials);

                appConfigOptions.ConfigureKeyVault(keyVaultOptions =>
                {
                    keyVaultOptions.SetCredential(credentials);
                });
            });

            // ensures that app configuration properties wont override local environment settings
            ChangeAppConfigurationSourcePriority(builder.ConfigurationBuilder.Sources);
        }

        if (IsDevelopment())
        {
            // allows configuration binding outside "Values" scope in local.settings.json
            builder.ConfigurationBuilder.AddJsonFile(
                new PhysicalFileProvider(context.ApplicationRootPath),
                path: "local.settings.json",
                optional: true,
                reloadOnChange: true);
        }

        static string? TryGetAppConfigurationEndpoint()
        {
            if (IsDevelopment() ||
                (Environment.GetEnvironmentVariable("APPCONFIG_EXTENSION_ENABLED") is string extensionEnabledValue &&
                bool.TryParse(extensionEnabledValue, out var extensionEnabled) &&
                extensionEnabled))
            {
                return Environment.GetEnvironmentVariable("APPCONFIG_ENDPOINT");
            }

            return null;
        }

        static void ChangeAppConfigurationSourcePriority(IList<IConfigurationSource> configSources)
        {
            var appConfigSource = configSources[^1];

            configSources.RemoveAt(configSources.Count - 1);
            configSources.Insert(configSources.Count - 1, appConfigSource);
        }

        static bool IsDevelopment()
        {
            return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
        }
    }

    public void Configure(IWebJobsBuilder builder)
    {
        builder.Services.AddSingleton<INameResolver, ImprovedNameResolver>();
    }
}
