using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.FileProviders;

namespace AzureFunctions.Worker.Extensions.AppConfiguration.Host;

/// <summary>
/// Wraps Configuration Source with <see cref="JsonConfigurationSource"/> to cache result.
/// If cache exists - returns cached values immediately and refreshes internal source in background.
/// If cache missing - loads data from internal source synchronously and updates cache.
/// </summary>
internal sealed class CachedConfigurationSource : IConfigurationSource
{
    private readonly IConfigurationSource internalSource;
    private readonly string cacheFilePath;
    private readonly JsonConfigurationSource cacheSource;

    public CachedConfigurationSource(
        IConfigurationSource internalSource,
        string cacheFileId)
    {
        this.internalSource = internalSource;

        this.cacheFilePath = PathHelper
            .GetSecretsPathFromSecretsId(cacheFileId)
            .Replace("secrets.json", $"{cacheFileId}.json");

        var directoryPath = Path.GetDirectoryName(this.cacheFilePath)!;

        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        this.cacheSource = new JsonConfigurationSource
        {
            Path = Path.GetFileName(this.cacheFilePath),
            FileProvider = new PhysicalFileProvider(directoryPath),
            Optional = true,
            ReloadOnChange = true,
        };
    }

    public IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        if (File.Exists(this.cacheFilePath))
        {
            Task.Run(FlushConfigurationToFile);
        }
        else
        {
            FlushConfigurationToFile();
        }

        return this.cacheSource.Build(builder);

        void FlushConfigurationToFile()
        {
            var provider = this.internalSource.Build(builder);

            provider.Load();

            using var stream = File.Open(this.cacheFilePath, FileMode.Create);

            JsonSerializer.Serialize(stream, provider.GetData());
        }
    }
}
