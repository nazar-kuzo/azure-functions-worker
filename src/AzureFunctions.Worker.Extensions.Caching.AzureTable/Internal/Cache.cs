using Azure;
using Azure.Data.Tables;

namespace AzureFunctions.Worker.Extensions.Caching.AzureTable.Internal;

/// <summary>
/// Cache inherited from <see cref="Dictionary{TKey, TValue}"/> will have better performance.
/// </summary>
internal sealed class Cache : Dictionary<string, object>, ITableEntity
{
    public required string PartitionKey
    {
        get => this.TryGetValue<string>(nameof(this.PartitionKey))!;
        set => this[nameof(this.PartitionKey)] = value;
    }

    public required string RowKey
    {
        get => this.TryGetValue<string>(nameof(this.RowKey))!;
        set => this[nameof(this.RowKey)] = value;
    }

    public byte[]? Data
    {
        get => this.TryGetValue<byte[]>(nameof(this.Data))!;
        set => this[nameof(this.Data)] = value!;
    }

    public DateTimeOffset? AbsoluteExpiration
    {
        get => this.TryGetValue<DateTimeOffset?>(nameof(this.AbsoluteExpiration));
        set => this[nameof(this.AbsoluteExpiration)] = value!;
    }

    public DateTimeOffset ExpiresAtTime
    {
        get => this.TryGetValue<DateTimeOffset>(nameof(this.ExpiresAtTime));
        set => this[nameof(this.ExpiresAtTime)] = value;
    }

    public long? SlidingExpirationInSeconds
    {
        get => this.TryGetValue<long?>(nameof(this.SlidingExpirationInSeconds));
        set => this[nameof(this.SlidingExpirationInSeconds)] = value!;
    }

    DateTimeOffset? ITableEntity.Timestamp
    {
        get => this.TryGetValue<DateTimeOffset?>(nameof(ITableEntity.Timestamp));
        set => this[nameof(ITableEntity.Timestamp)] = value!;
    }

    ETag ITableEntity.ETag
    {
        get => this.TryGetValue<ETag>("odata.etag");
        set => this["odata.etag"] = value;
    }

    private T? TryGetValue<T>(string key)
    {
        return this.TryGetValue(key, out var value) ? (T) value : default;
    }
}
