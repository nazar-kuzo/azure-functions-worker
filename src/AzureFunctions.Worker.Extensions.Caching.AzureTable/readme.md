## [Distributed caching with Azure Table storage](src/AzureFunctions.Worker.Extensions.Caching.AzureTable/readme.md)
- Use Azure Table as distributed cache provider
- Full support for `IDistributedCache` interface
- JSON serialization extensions with host `System.Text.Json` serializer options as default
- Search cache entries or bulk set/delete operations support

## NuGet package
[https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.Caching.AzureTable](https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.Caching.AzureTable)

## Example

Program.cs
```csharp
var builder = FunctionsApplication.CreateBuilder(args);

// should be used for HTTP triggered APIs
builder.ConfigureFunctionsWebApplication();

builder.AddAzureTableCache(cacheOptions =>
{
    cacheOptions.ConnectionString = builder.Configuration.GetConnectionString("AzureWebJobsStorage");
    cacheOptions.ApplicationName = "Default";
    cacheOptions.TableName = "Cache";
    cacheOptions.ExpiredItemsDeletionInterval = TimeSpan.FromMinutes(30);
    cacheOptions.DefaultSlidingExpiration = TimeSpan.FromMinutes(20);
});

// configure host JSON serialization options
builder.Services.Configure<JsonSerializerOptions>(jsonOptions => {
    jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    jsonOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});
```

Usage
```csharp
public class UserService(IDistributedCache cache)
{
    public async Task<UserInfo> GetUserAsync(string userId)
    {
        return await cache.GetJsonAsync<UserInfo>(userId);
    }

    public async Task SetUserAsync(UserInfo user)
    {
        await cache.SetJsonAsync(user.Id, user, new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) });
    }
}
```