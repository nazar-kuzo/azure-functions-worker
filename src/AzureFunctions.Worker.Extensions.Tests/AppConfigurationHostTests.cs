using AzureFunctions.Worker.Extensions.AppConfiguration.Host;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.Configuration;

namespace AzureFunctions.Worker.Extensions.Tests;

public class AppConfigurationHostTests
{
    [Fact]
    public void AppConfigurationHost_CacheWorks()
    {
        var configuration = new WebJobsConfiguration();
        var builderContext = new WebJobsBuilderContext()
        {
            ApplicationRootPath = AppDomain.CurrentDomain.BaseDirectory,
        };

        Environment.SetEnvironmentVariable("DOTNET_ENVIRONMENT", "Development");
        Environment.SetEnvironmentVariable("APPCONFIG_ENDPOINT", "https://myappconfig.azconfig.io");

        configuration.ConfigurationBuilder.AddEnvironmentVariables();

        var hostStartup = new AppConfigurationHostStartup();

        hostStartup.Configure(builderContext, configuration);

        var configurationRoot = configuration.ConfigurationBuilder.Build();

        var configSection = configurationRoot.GetSection("Some:NestedValue");

        Assert.True(configSection.Exists());
    }
}

file class WebJobsConfiguration : IWebJobsConfigurationBuilder
{
    private readonly IConfigurationBuilder configurationBuilder = new ConfigurationBuilder();

    public IConfigurationBuilder ConfigurationBuilder => this.configurationBuilder;
}