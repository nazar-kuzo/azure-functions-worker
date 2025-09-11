## [Lightweight DurableTask client](src/AzureFunctions.Worker.Extensions.DurableTask.Client/readme.md)
- Start orchestrations that are hosted in different Azure Functions
- Avoid including redundant packages if your function is not hosting DurableTask orchestreation
- Connect to external DurableTask hub via ConnectionString

## NuGet package
[https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.DurableTask.Client](https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.DurableTask.Client)

## Example

Program.cs
```csharp
builder.ConfigureDurableTaskClient()
```

Function.cs
```csharp
public sealed class DurableTask
{
    [Function(nameof(DurableTask) + "_" + nameof(GetInstances))]
    public Task<IEnumerable<OrchestrationState>> GetInstances(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "durable-task/instances")] HttpRequest request,
        [DurableClient] DurableTaskClient durableTaskClient,
        [FromQuery, Required] string prefix)
    {
        return durableTaskClient.ListInstancesAsync(prefix);
    }
}
```