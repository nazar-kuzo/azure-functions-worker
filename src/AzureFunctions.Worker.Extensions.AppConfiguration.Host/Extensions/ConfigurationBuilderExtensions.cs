using Azure.Core;
using Microsoft.Extensions.Configuration.AzureAppConfiguration;
using Microsoft.Extensions.Configuration.EnvironmentVariables;

namespace Microsoft.Extensions.Configuration;

internal static class ConfigurationBuilderExtensions
{
    /// <summary>
    /// Adds AzureAppConfiguration source right before <see cref="EnvironmentVariablesConfigurationSource"/>
    /// so local environment variables, local settings and user secrets will have higher priority
    /// </summary>
    /// <param name="builder">Configuration builder</param>
    /// <param name="appConfigUri">Azure App Configuration Endpoint URI</param>
    /// <param name="credential">Custom credentials to connect to App Configuration</param>
    /// <param name="useCache">Indicates whether App Configuration values should be cached locally</param>
    /// <param name="configureAppConfigurationOptions">Action to configure <see cref="AzureAppConfigurationOptions"/></param>
    /// <param name="configureKeyVaultOptions">Action to configure <see cref="AzureAppConfigurationKeyVaultOptions"/></param>
    /// <returns>Configuration builder with added App Configuration source</returns>
    public static IConfigurationBuilder AddAzureAppConfiguration(
        this IConfigurationBuilder builder,
        Uri appConfigUri,
        TokenCredential credential,
        bool useCache,
        Action<AzureAppConfigurationOptions>? configureAppConfigurationOptions = null,
        Action<AzureAppConfigurationKeyVaultOptions>? configureKeyVaultOptions = null)
    {
        var appConfigurationSource = CreateAzureAppConfigurationSource(appConfigOptions =>
        {
            appConfigOptions.Connect(appConfigUri, credential);

            appConfigOptions.ConfigureKeyVault(keyVaultOptions =>
            {
                keyVaultOptions.SetCredential(credential);

                configureKeyVaultOptions?.Invoke(keyVaultOptions);
            });

            configureAppConfigurationOptions?.Invoke(appConfigOptions);
        });

        if (useCache)
        {
            appConfigurationSource = new CachedConfigurationSource(
                appConfigurationSource,
                cacheId: appConfigUri.Host);
        }

        var environmentConfigurationSource = builder.Sources
            .OfType<EnvironmentVariablesConfigurationSource>()
            .First();

        // ensures that app configuration properties wont override local environment settings
        builder.Sources.Insert(
            builder.Sources.IndexOf(environmentConfigurationSource),
            appConfigurationSource);

        return builder;
    }

    /// <summary>
    /// Creates internal AzureAppConfigurationSource so it can be
    /// wrapped later with <see cref="CachedConfigurationSource"/>
    /// </summary>
    /// <param name="appConfigurationOptionsConfigurator">Action to configure <see cref="AzureAppConfigurationOptions"/></param>
    /// <param name="optional">Whether this configuration source is optional</param>
    /// <returns>Internal AzureAppConfigurationSource</returns>
    private static IConfigurationSource CreateAzureAppConfigurationSource(
        Action<AzureAppConfigurationOptions> appConfigurationOptionsConfigurator,
        bool optional = false)
    {
        var appConfigurationSourceType = typeof(AzureAppConfigurationOptions).Assembly
            .GetType("Microsoft.Extensions.Configuration.AzureAppConfiguration.AzureAppConfigurationSource")!;

        return (IConfigurationSource) Activator.CreateInstance(appConfigurationSourceType, [appConfigurationOptionsConfigurator, optional])!;
    }
}
