namespace Microsoft.Extensions.Caching.Distributed;

public sealed class AzureTableCacheOptions
{
    public string? ConnectionString { get; set; }

    public string TableName { get; set; } = "Cache";

    public string ApplicationName { get; set; } = "Default";

    public TimeSpan ExpiredItemsDeletionInterval { get; set; } = TimeSpan.FromMinutes(30);

    public TimeSpan DefaultSlidingExpiration { get; set; } = TimeSpan.FromMinutes(20);
}