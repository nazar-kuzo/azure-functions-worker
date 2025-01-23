using System.Text;
using Azure;
using Azure.Data.Tables;
using AzureFunctions.Worker.Extensions.Caching.AzureTable.Internal;

namespace Microsoft.Extensions.Caching.Distributed;

internal sealed class AzureTableCache(
    IOptions<AzureTableCacheOptions> cacheOptions,
    IOptions<JsonSerializerOptions> serializerOptions)
    : IDistributedCache
{
    private static readonly string[] CacheWithoutValue =
        [nameof(Cache.PartitionKey), nameof(Cache.RowKey), nameof(Cache.ExpiresAtTime), nameof(Cache.SlidingExpirationInSeconds)];

    private readonly TableClient tableClient = CreateTableClient(cacheOptions.Value.ConnectionString!, cacheOptions.Value.TableName);

    private readonly Lock expirationLock = new();

    private DateTimeOffset? lastExpirationScan;

    #region Extensions

    public async Task<List<T>> SearchJsonAsync<T>(string keyPattern, CancellationToken cancellationToken = default)
    {
        this.ScheduleExpiredCacheCleanup();

        var pages = this.tableClient.QueryAsync<Cache>(this.BuildFilter(keyPattern), maxPerPage: 1000, cancellationToken: cancellationToken);

        var values = new List<T>();

        await foreach (var page in pages.AsPages())
        {
            this.ScheduleCacheRefresh(page.Values);

            values.AddRange(page.Values.Select(cache => JsonSerializer.Deserialize<T>(cache.Data, serializerOptions.Value)!));
        }

        return values;
    }

    public async Task<T?> GetJsonAsync<T>(string key, CancellationToken token = default)
    {
        this.ScheduleExpiredCacheCleanup();

        var cache = await this.RetrieveAsync(key, cancellationToken: token);

        if (cache != null)
        {
            this.ScheduleCacheRefresh(cache);

            return JsonSerializer.Deserialize<T>(cache.Data, serializerOptions.Value)!;
        }

        return default;
    }

    public Task SetJsonAsync<T>(
        string key,
        T value,
        DistributedCacheEntryOptions cacheOptions,
        CancellationToken token = default)
    {
        var data = JsonSerializer.SerializeToUtf8Bytes(value, serializerOptions.Value);

        return this.SetAsync(key, data, cacheOptions, token);
    }

    public async Task SetAllJsonAsync<T>(
        Dictionary<string, T> entries,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        this.ScheduleExpiredCacheCleanup();

        var now = DateTime.UtcNow;

        foreach (var entriesChunk in entries.Chunk(100))
        {
            var actions = entriesChunk.Select(entry =>
            {
                var data = JsonSerializer.SerializeToUtf8Bytes(entry.Value, serializerOptions.Value);

                return new TableTransactionAction(
                    TableTransactionActionType.UpsertReplace,
                    this.CreateCache(entry.Key, data, options, now));
            });

            await this.tableClient.SubmitTransactionAsync(actions, token);
        }
    }

    public async Task RemoveAllAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        this.ScheduleExpiredCacheCleanup();

        if (keys.Any())
        {
            var entriesToRemove = keys.Select(key =>
            {
                return new TableTransactionAction(
                    TableTransactionActionType.Delete,
                    new Cache
                    {
                        PartitionKey = cacheOptions.Value.ApplicationName,
                        RowKey = key,
                    });
            });

            foreach (var entries in entriesToRemove.Chunk(100))
            {
                await this.tableClient.SubmitTransactionAsync(entries, cancellationToken);
            }
        }
    }

    public async Task RemoveAllAsync(
        string keyPattern,
        CancellationToken cancellationToken = default)
    {
        this.ScheduleExpiredCacheCleanup();

        var pages = this.tableClient.QueryAsync<Cache>(this.BuildFilter(keyPattern), maxPerPage: 100, CacheWithoutValue, cancellationToken);

        await foreach (var page in pages.AsPages())
        {
            var actions = page.Values.Select(cache => new TableTransactionAction(TableTransactionActionType.Delete, cache));

            await this.tableClient.SubmitTransactionAsync(actions, cancellationToken);
        }
    }

    #endregion

    #region Asynchronous API

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        this.ScheduleExpiredCacheCleanup();

        var cache = await this.RetrieveAsync(key, cancellationToken: token);

        if (cache != null)
        {
            this.ScheduleCacheRefresh(cache);
        }

        return cache?.Data;
    }

    public Task SetAsync(
        string key,
        byte[] value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        this.ScheduleExpiredCacheCleanup();

        var cache = this.CreateCache(key, value, options, DateTime.UtcNow);

        return this.tableClient.UpsertEntityAsync(cache, TableUpdateMode.Replace);
    }

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        this.ScheduleExpiredCacheCleanup();

        var cache = await this.RetrieveAsync(key, CacheWithoutValue, token);

        if (cache != null)
        {
            this.ScheduleCacheRefresh(cache);
        }
    }

    public Task RemoveAsync(string key, CancellationToken token = default)
    {
        this.ScheduleExpiredCacheCleanup();

        return this.tableClient.DeleteEntityAsync(cacheOptions.Value.ApplicationName, key, cancellationToken: token);
    }

    #endregion

    #region Synchronous API

    public byte[]? Get(string key)
    {
        return this.GetAsync(key).GetAwaiter().GetResult();
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        this.SetAsync(key, value, options).GetAwaiter().GetResult();
    }

    public void Refresh(string key)
    {
        this.RefreshAsync(key).GetAwaiter().GetResult();
    }

    public void Remove(string key)
    {
        this.RemoveAsync(key).GetAwaiter().GetResult();
    }

    #endregion

    private static TableClient CreateTableClient(string connectionString, string tableName)
    {
        var retryPolicy = new DefaultRetryPolicy();

        var clientOptions = new TableClientOptions
        {
            RetryPolicy = retryPolicy,
            Diagnostics =
            {
                LoggedHeaderNames = { "x-ms-request-id", "DataServiceVersion" },
                LoggedQueryParameters = { "api-version", "$format", "$filter", "$top", "$select" },
            },
        };

        var serviceClient = new TableServiceClient(connectionString, clientOptions);

        retryPolicy.ErrorHandlers.Add("TableNotFound", () => serviceClient.CreateTableIfNotExistsAsync(tableName));

        return serviceClient.GetTableClient(tableName);
    }

    private Cache CreateCache(string key, byte[] value, DistributedCacheEntryOptions options, DateTime now)
    {
        var cache = new Cache()
        {
            PartitionKey = cacheOptions.Value.ApplicationName,
            RowKey = key,
            Data = value,
        };

        var absoluteExpiration = options.AbsoluteExpiration.HasValue || options.AbsoluteExpirationRelativeToNow.HasValue
            ? options.AbsoluteExpiration ?? now.Add(options.AbsoluteExpirationRelativeToNow!.Value)
            : (DateTimeOffset?) null;

        var slidingExpirationInSeconds = options.SlidingExpiration.HasValue
            ? options.SlidingExpiration.Value.TotalSeconds
            : !absoluteExpiration.HasValue
                ? cacheOptions.Value.DefaultSlidingExpiration.TotalSeconds
                : (double?) null;

        var expiresAtTime = slidingExpirationInSeconds.HasValue
            ? now.AddSeconds(slidingExpirationInSeconds.Value)
            : absoluteExpiration!.Value;

        if (expiresAtTime > absoluteExpiration)
        {
            expiresAtTime = absoluteExpiration.Value;
        }

        if (absoluteExpiration.HasValue)
        {
            cache.AbsoluteExpiration = absoluteExpiration.Value;
        }

        if (slidingExpirationInSeconds.HasValue)
        {
            cache.SlidingExpirationInSeconds = (long) slidingExpirationInSeconds.Value;
        }

        cache.ExpiresAtTime = expiresAtTime;

        return cache;
    }

    private async Task<Cache?> RetrieveAsync(string key, IEnumerable<string>? select = null, CancellationToken cancellationToken = default)
    {
        var response = this.tableClient
            .QueryAsync<Cache>(this.BuildFilter(key), maxPerPage: 1, select, cancellationToken)
            .AsPages()
            .GetAsyncEnumerator();

        await response.MoveNextAsync();

        return response.Current.Values.FirstOrDefault();
    }

    private string BuildFilter(string keyPattern)
    {
        var now = DateTime.UtcNow;

        var stringBuilder = new StringBuilder(
            $"{nameof(Cache.PartitionKey)} eq '{cacheOptions.Value.ApplicationName}'" +
            $" and {nameof(Cache.ExpiresAtTime)} gt datetime'{now:yyyy-MM-dd'T'HH:mm:ssZ}'");

        if (keyPattern.EndsWith('*'))
        {
            var startsWithPhrase = keyPattern[..keyPattern.IndexOf('*')];

            if (!string.IsNullOrEmpty(startsWithPhrase))
            {
                var startsWithEndPhrase = startsWithPhrase[0..^1] + (char) (startsWithPhrase[^1] + 1);

                // table storage doesn't support StartsWith Comparison Operators
                // so we simulate GreaterThanOrEqual and LessThan comparisons on string range
                stringBuilder.Append(
                    $" and {nameof(Cache.RowKey)} ge '{startsWithPhrase}'" +
                    $" and {nameof(Cache.RowKey)} lt '{startsWithEndPhrase}'");
            }
        }
        else
        {
            stringBuilder.Append($" and {nameof(Cache.RowKey)} eq '{keyPattern}'");
        }

        return stringBuilder.ToString();
    }

    private void ScheduleExpiredCacheCleanup()
    {
        var now = DateTime.UtcNow;

        if (ShouldScanForExpiredEntries(now))
        {
            lock (this.expirationLock)
            {
                if (ShouldScanForExpiredEntries(now))
                {
                    this.lastExpirationScan = now;

                    Task.Factory
                        .StartNew(DeleteExpiredEntries, TaskCreationOptions.LongRunning)
                        .ConfigureAwait(false);
                }
            }
        }

        bool ShouldScanForExpiredEntries(DateTime now)
        {
            return this.lastExpirationScan == null || (now - this.lastExpirationScan) > cacheOptions.Value.ExpiredItemsDeletionInterval;
        }

        async Task DeleteExpiredEntries()
        {
            var entries = this.tableClient.QueryAsync<Cache>(
                $"{nameof(Cache.PartitionKey)} eq '{cacheOptions.Value.ApplicationName}'" +
                $" and {nameof(Cache.ExpiresAtTime)} le datetime'{now:yyyy-MM-dd'T'HH:mm:ssZ}'",
                maxPerPage: 100,
                select: CacheWithoutValue);

            await foreach (var page in entries.AsPages())
            {
                if (page.Values.Count > 0)
                {
                    await this.tableClient.SubmitTransactionAsync(
                        page.Values.Select(cache => new TableTransactionAction(TableTransactionActionType.Delete, cache)));
                }
            }
        }
    }

    private void ScheduleCacheRefresh(Cache cache)
    {
        if (cache.SlidingExpirationInSeconds.HasValue &&
            (cache.AbsoluteExpiration is null || cache.ExpiresAtTime < cache.AbsoluteExpiration.Value))
        {
            Task.Factory.StartNew(
                async cache =>
                {
                    var entry = (Cache) cache!;

                    var expiresAtTime = DateTimeOffset.UtcNow.AddSeconds(entry.SlidingExpirationInSeconds!.Value);

                    if (expiresAtTime > entry.AbsoluteExpiration)
                    {
                        expiresAtTime = entry.AbsoluteExpiration.Value;
                    }

                    await this.tableClient.UpdateEntityAsync(
                        new Cache
                        {
                            PartitionKey = entry.PartitionKey,
                            RowKey = entry.RowKey,
                            ExpiresAtTime = expiresAtTime,
                        },
                        ETag.All,
                        mode: TableUpdateMode.Merge);
                },
                cache);
        }
    }

    private void ScheduleCacheRefresh(IEnumerable<Cache> entries)
    {
        var entriesToRefresh = entries.Where(cache =>
            cache.SlidingExpirationInSeconds.HasValue &&
            (cache.AbsoluteExpiration is null || cache.ExpiresAtTime < cache.AbsoluteExpiration.Value));

        if (entriesToRefresh.Any())
        {
            Task.Factory.StartNew(
                async entriesToRefresh =>
                {
                    var now = DateTimeOffset.UtcNow;

                    foreach (var entriesChunk in ((IEnumerable<Cache>) entriesToRefresh!).Chunk(100))
                    {
                        await this.tableClient.SubmitTransactionAsync(entriesChunk.Select(entry =>
                        {
                            var expiresAtTime = now.AddSeconds(entry.SlidingExpirationInSeconds!.Value);

                            if (expiresAtTime > entry.AbsoluteExpiration)
                            {
                                expiresAtTime = entry.AbsoluteExpiration.Value;
                            }

                            return new TableTransactionAction(
                                TableTransactionActionType.UpdateMerge,
                                new Cache
                                {
                                    PartitionKey = entry.PartitionKey,
                                    RowKey = entry.RowKey,
                                    ExpiresAtTime = expiresAtTime,
                                });
                        }));
                    }
                },
                entriesToRefresh);
        }
    }
}
