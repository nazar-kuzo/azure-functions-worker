using Azure.Identity;
using AzureFunctions.Worker.Extensions.AppConfiguration.Host;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;

[assembly: WebJobsStartup(typeof(AppConfigurationHostStartup))]

namespace AzureFunctions.Worker.Extensions.AppConfiguration.Host;

public class AppConfigurationHostStartup : IWebJobsConfigurationStartup
{
    public void Configure(WebJobsBuilderContext context, IWebJobsConfigurationBuilder builder)
    {
        if (Environment.GetEnvironmentVariable("APPCONFIG_ENDPOINT") is string appConfigEndpoint)
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
        }
    }
}
