## [Lightweight DurableTask client for ASP.NET Core application](src/DurableTask.Client.AzureStorage/readme.md)
- Start orchestrations that are hosted in different Azure Functions
- Avoid including redundant packages for hosting DurableTask orchestreation
- Connect to external DurableTask hub via ConnectionString

## NuGet package
[https://www.nuget.org/packages/DurableTask.Client.AzureStorage](https://www.nuget.org/packages/DurableTask.Client.AzureStorage)

## Example

Program.cs
```csharp
builder.ConfigureDurableTaskClient()
```

Controller.cs
```csharp
public sealed class Controller(DurableTaskClient durableTaskClient)
{
    [HttpGet("")]
    public Task<IEnumerable<OrchestrationState>> GetInstances(
        [FromQuery, Required] string prefix)
    {
        return durableTaskClient.ListInstancesAsync(prefix);
    }
}
```