namespace Microsoft.Extensions.Caching.Distributed;

public static class DistributedCacheExtensions
{
    /// <summary>
    /// Gets JSON value from cache
    /// </summary>
    /// <typeparam name="T">Value Type</typeparam>
    /// <param name="cache">IDistributedCache service</param>
    /// <param name="key">Cache key</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Task</returns>
    public static async Task<T?> GetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        CancellationToken token = default)
    {
        return await TryGetAzureTableCache(cache).GetJsonAsync<T>(key, token);
    }

    /// <summary>
    /// Gets a list of cached items based on specified key wildcard pattern
    /// </summary>
    /// <typeparam name="T">Value Type</typeparam>
    /// <param name="cache">IDistributedCache service</param>
    /// <param name="keyPattern">Key wildcard pattern</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>List of cached items</returns>
    public static Task<List<T>> SearchJsonAsync<T>(
        this IDistributedCache cache,
        string keyPattern,
        CancellationToken token = default)
    {
        return TryGetAzureTableCache(cache).SearchJsonAsync<T>(keyPattern, token);
    }

    /// <summary>
    /// Sets value into cache as JSON
    /// </summary>
    /// <typeparam name="T">Value Type</typeparam>
    /// <param name="cache">IDistributedCache service</param>
    /// <param name="key">Cache key</param>
    /// <param name="value">Cache value</param>
    /// <param name="options">DistributedCache options</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Task</returns>
    public static Task SetJsonAsync<T>(
        this IDistributedCache cache,
        string key,
        T value,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        return TryGetAzureTableCache(cache).SetJsonAsync(key, value, options, token);
    }

    /// <summary>
    /// Sets cache entries into cache as JSON
    /// </summary>
    /// <typeparam name="T">Value Type</typeparam>
    /// <param name="cache">IDistributedCache service</param>
    /// <param name="entries">Cache entries</param>
    /// <param name="options">DistributedCache options</param>
    /// <param name="token">Cancellation token</param>
    /// <returns>Task</returns>
    public static Task SetAllJsonAsync<T>(
        this IDistributedCache cache,
        Dictionary<string, T> entries,
        DistributedCacheEntryOptions options,
        CancellationToken token = default)
    {
        return TryGetAzureTableCache(cache).SetAllJsonAsync(entries, options, token);
    }

    /// <summary>
    /// Remove multiple cache entries in bulk
    /// </summary>
    /// <param name="cache">IDistributedCache service</param>
    /// <param name="keys">Cache entry keys</param>
    /// <param name="cancellationToken">Cancellation Token</param>
    /// <returns>Task</returns>
    public static Task RemoveAllAsync(
        this IDistributedCache cache,
        IEnumerable<string> keys,
        CancellationToken cancellationToken = default)
    {
        return TryGetAzureTableCache(cache).RemoveAllAsync(keys, cancellationToken);
    }

    /// <summary>
    /// Removes all cached items based on specified key wildcard pattern
    /// </summary>
    /// <param name="cache">IDistributedCache service</param>
    /// <param name="keyPattern">Key wildcard pattern</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    public static Task RemoveAllAsync(
        this IDistributedCache cache,
        string keyPattern,
        CancellationToken cancellationToken = default)
    {
        return TryGetAzureTableCache(cache).RemoveAllAsync(keyPattern, cancellationToken);
    }

    private static AzureTableCache TryGetAzureTableCache(IDistributedCache cache)
    {
        if (cache is not AzureTableCache azureTableCache)
        {
            throw new NotSupportedException("IDistributedCache should be implemented by AzureTableCache");
        }

        return azureTableCache;
    }
}
