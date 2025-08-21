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
        var isDevelopment = Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") == "Development";

        if (TryGetAppConfigurationEndpoint() is Uri appConfigEndpoint)
        {
            var credentialOptions = new DefaultAzureCredentialOptions();

            if (Environment.GetEnvironmentVariable("APPCONFIG_TENANT") is string tenantId)
            {
                credentialOptions.TenantId = tenantId;
            }

            try
            {
                builder.ConfigurationBuilder.AddAzureAppConfiguration(
                    appConfigEndpoint,
                    new DefaultAzureCredential(credentialOptions),
                    useCache: isDevelopment);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{nameof(AppConfigurationHostStartup)}: failed to initialize Azure App Configuration");
                Console.WriteLine(ex);
            }
        }

        if (isDevelopment)
        {
            // allows configuration binding outside "Values" scope in local.settings.json
            builder.ConfigurationBuilder.AddJsonFile(
                new PhysicalFileProvider(context.ApplicationRootPath),
                path: "local.settings.json",
                optional: true,
                reloadOnChange: true);
        }

        Uri? TryGetAppConfigurationEndpoint()
        {
            var extensionEnabledValue = Environment.GetEnvironmentVariable("APPCONFIG_EXTENSION_ENABLED");

            if (isDevelopment || extensionEnabledValue?.Equals("true", StringComparison.InvariantCultureIgnoreCase) == true)
            {
                var appConfigEndpoint = Environment.GetEnvironmentVariable("APPCONFIG_ENDPOINT");

                return Uri.TryCreate(appConfigEndpoint, UriKind.Absolute, out var appConfigUri)
                    ? appConfigUri
                    : null;
            }

            return null;
        }
    }

    public void Configure(IWebJobsBuilder builder)
    {
        // improves existing name resolver with ability to resolve nested settings
        builder.Services.AddSingleton<INameResolver, ImprovedNameResolver>();
    }
}
