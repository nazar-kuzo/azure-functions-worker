using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.Configuration;

public static class AppConfigurationExtensions
{
    /// <summary>
    /// Connects to Azure App Configuration service based on endpoint provided in environment variable
    /// using <see cref="DefaultAzureCredential"/>.
    /// To authenticate successfully, your Azure account must have the "App Configuration Data Reader"
    /// role assigned on the target App Configuration instance.
    /// </summary>
    /// <param name="appBuilder">Configuration manager provided by host</param>
    /// <param name="appConfigEndpointVariableName">Environment name for App Configuration Endpoint</param>
    /// <param name="credential">Custom TokenCredential configuration</param>
    /// <param name="configureAppConfigurationOptions">Action to configure <see cref="AzureAppConfigurationOptions"/></param>
    /// <param name="configureKeyVaultOptions">Action to configure <see cref="AzureAppConfigurationKeyVaultOptions"/></param>
    /// <returns><see cref="IHostApplicationBuilder"/> with additional configuration sources registered</returns>
    public static IHostApplicationBuilder BootstrapAppConfiguration(
        this IHostApplicationBuilder appBuilder,
        string appConfigEndpointVariableName = "APPCONFIG_ENDPOINT",
        TokenCredential? credential = null,
        Action<AzureAppConfigurationOptions>? configureAppConfigurationOptions = null,
        Action<AzureAppConfigurationKeyVaultOptions>? configureKeyVaultOptions = null)
    {
        var appConfigEndpoint = Environment.GetEnvironmentVariable(appConfigEndpointVariableName);

        if (Uri.TryCreate(appConfigEndpoint, UriKind.Absolute, out var appConfigEndpointUri))
        {
            var credentialOptions = new DefaultAzureCredentialOptions();

            if (Environment.GetEnvironmentVariable("APPCONFIG_TENANT") is string tenantId)
            {
                credentialOptions.TenantId = tenantId;
            }

            credential = credential ?? new DefaultAzureCredential(credentialOptions);

            appBuilder.Configuration.AddAzureAppConfiguration(
                appConfigEndpointUri,
                credential,
                useCache: appBuilder.Environment.IsDevelopment(),
                configureAppConfigurationOptions,
                configureKeyVaultOptions);
        }

        return appBuilder;
    }
}
