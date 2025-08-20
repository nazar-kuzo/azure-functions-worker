using System.Runtime.CompilerServices;

namespace Microsoft.Extensions.Configuration;

internal static class ConfigurationProviderExtensions
{
    /// <summary>
    /// Gets the configuration key-value pairs for this provider.
    /// </summary>
    /// <param name="provider">ConfigurationProvider</param>
    /// <returns>Configuration key-value pairs</returns>
    public static IDictionary<string, string?> GetData(this IConfigurationProvider provider)
    {
        return GetDataInternal((ConfigurationProvider) provider);
    }

    [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "get_Data")]
    private static extern IDictionary<string, string?> GetDataInternal(ConfigurationProvider provider);
}
