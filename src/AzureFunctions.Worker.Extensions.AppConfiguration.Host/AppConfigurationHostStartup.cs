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
        Thread.Sleep(10_000);

        if (Environment.GetEnvironmentVariable("APPCONFIG_ENDPOINT") is string appConfigEndpoint)
        {
            builder.ConfigurationBuilder.AddAzureAppConfiguration(appConfigOptions =>
            {
                appConfigOptions.Connect(new Uri(appConfigEndpoint), new DefaultAzureCredential())
                    .Select("*", tagFilters: ["Host=true"]);

                appConfigOptions.ConfigureKeyVault(keyVaultOptions =>
                {
                    keyVaultOptions.SetCredential(new DefaultAzureCredential());
                });
            });
        }
    }
}
