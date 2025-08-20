using Azure.Identity;
using AzureFunctions.Worker.Extensions.AppConfiguration.Host;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

[assembly: WebJobsStartup(typeof(AppConfigurationHostStartup))]

namespace AzureFunctions.Worker.Extensions.AppConfiguration.Host;

public class AppConfigurationHostStartup : IWebJobsStartup, IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
        if (TryGetAppConfigurationEndpoint() is string appConfigEndpoint &&
            Uri.TryCreate(appConfigEndpoint, UriKind.Absolute, out var appConfigUri))
        {
            var credentialOptions = new DefaultAzureCredentialOptions();

            if (Environment.GetEnvironmentVariable("APPCONFIG_TENANT") is string tenantId)
            {
                credentialOptions.TenantId = tenantId;
            }

            var credentials = new DefaultAzureCredential(credentialOptions);

            var appConfigurationSource = CreateAzureAppConfigurationSource(
                appConfigUri,
                appConfigOptions =>
                {
                    appConfigOptions.Connect(appConfigUri, credentials);

                    appConfigOptions.ConfigureKeyVault(keyVaultOptions =>
                    {
                        keyVaultOptions.SetCredential(credentials);
                    });
                });

            var environmentConfigurationSource = builder.ConfigurationBuilder.Sources
                .OfType<EnvironmentVariablesConfigurationSource>()
                .First();

            // ensures that app configuration properties wont override local environment settings
            builder.ConfigurationBuilder.Sources.Insert(
                builder.ConfigurationBuilder.Sources.IndexOf(environmentConfigurationSource),
                appConfigurationSource);
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

        static bool IsDevelopment()
        {
            return Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";
        }

        static IConfigurationSource CreateAzureAppConfigurationSource(
            Uri appConfigUri,
            Action<AzureAppConfigurationOptions> action,
            bool optional = false)
        {
            var appConfigurationSourceType = typeof(AzureAppConfigurationOptions).Assembly
                .GetType("Microsoft.Extensions.Configuration.AzureAppConfiguration.AzureAppConfigurationSource")!;

            var configurationSource = (IConfigurationSource) Activator.CreateInstance(appConfigurationSourceType, [action, optional])!;

            if (IsDevelopment())
            {
                configurationSource = new CachedConfigurationSource(
                    configurationSource,
                    Environment.GetEnvironmentVariable("APPCONFIG_CACHE_ID") ?? appConfigUri.Host);
            }

            return configurationSource;
        }
    }

    public void Configure(IWebJobsBuilder builder)
    {
        builder.Services.AddSingleton<INameResolver, ImprovedNameResolver>();
    }
}
