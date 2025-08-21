using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Configuration.UserSecrets;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.Extensions.Configuration;

/// <summary>
/// Wraps Configuration Source with <see cref="JsonConfigurationSource"/> to cache result.
/// If cache exists - returns cached values immediately and refreshes internal source in background.
/// If cache missing - loads data from internal source synchronously and updates cache.
/// </summary>
internal sealed class CachedConfigurationSource : IConfigurationSource
{
    private static readonly JsonSerializerOptions CacheSerializerOptions = new() { WriteIndented = true };

    private readonly IConfigurationSource internalSource;
    private readonly string cacheFilePath;
    private readonly JsonConfigurationSource cacheSource;

    public CachedConfigurationSource(
        IConfigurationSource internalSource,
        string cacheId)
    {
        this.internalSource = internalSource;
        this.cacheFilePath = PathHelper.GetSecretsPathFromSecretsId(cacheId);

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
            Task.Run(LoadConfigurationToFile);
        }
        else
        {
            LoadConfigurationToFile();
        }

        return this.cacheSource.Build(builder);

        void LoadConfigurationToFile()
        {
            var provider = this.internalSource.Build(builder);

            try
            {
                provider.Load();

                using var stream = File.Open(this.cacheFilePath, FileMode.Create);

                JsonSerializer.Serialize(stream, provider.GetData(), CacheSerializerOptions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"{nameof(CachedConfigurationSource)}: failed to load internal configuration");
                Console.WriteLine(ex);
            }
        }
    }

    public override string ToString()
    {
        return $"{typeof(CachedConfigurationSource).FullName}<{this.internalSource}>";
    }
}
