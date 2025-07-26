## [Type safe DurableTasks](src/AzureFunctions.Worker.Extensions.DurableTask/readme.md)
- Call orchestrations and activities with expressions to improve type safety
- Avoid hardcoding function names with strings
- Less boilerplate with useful extensions

## NuGet package
[https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.DurableTask](https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.DurableTask)

## Example

Usage
```csharp
public class DurableTasks
{
    [Function(nameof(DurableTasks) + "_" + nameof(Orchestration))]
    public async Task Orchestration(
        [OrchestrationTrigger] TaskOrchestrationContext context,
        Data data)
    {
        // compile-time type safety check on function input and output parameter
        var result = await context.CallActivityAsync(() => this.SingleParamActivity(data));

        // workaround to pass activity context as default parameter in parameterless activity
        await context.CallActivityAsync(() => this.EmptyActivity(ActivityContext.Default));

        // workaround to pass multiple parameters into activity
        await context.CallActivityAsync(() => this.MultipleParamsActivity(ValueTuple.Create(data, Guid.NewGuid())));
    }

    [Function(nameof(DurableTasks) + "_" + nameof(SingleParamActivity))]
    public Task<Result> SingleParamActivity([ActivityTrigger] Data data)
    {
        return Task.FromResult(new Result());
    }

    [Function(nameof(DurableTasks) + "_" + nameof(EmptyActivity))]
    public Task EmptyActivity([ActivityTrigger] TaskActivityContext context)
    {
        return Task.CompletedTask;
    }

    [Function(nameof(DurableTasks) + "_" + nameof(MultipleParamsActivity))]
    public Task MultipleParamsActivity([ActivityTrigger] (Data Data, Guid Id) context)
    {
        return Task.CompletedTask;
    }
}
```