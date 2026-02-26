using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

/// <summary>
/// Starts <see cref="RequestTelemetry"/> operation based on worker host activity.
/// Request telemetry could be hijacked by HTTP middleware if function is triggered by HTTP
/// </summary>
/// <param name="telemetryClient">Telemetry client</param>
/// <param name="activityCoordinator">Function activity coordinator. Could be null if &quot;enableHttpRequestMapping&quot; is set to false</param>
internal class FunctionRequestTelemetryMiddleware(
    TelemetryClient telemetryClient,
    HttpRequestActivityCoordinator? activityCoordinator = null)
    : IFunctionsWorkerMiddleware
{
    private readonly ConcurrentDictionary<string, string[]> functionTriggers = new();

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // durable functions distributed logging is handled on the host
        if (FunctionContainsTriggers("orchestrationTrigger", "activityTrigger"))
        {
            await next(context);

            return;
        }

        var hostActivity = Activity.Current!;
        var shouldDelegateRequestActivity = activityCoordinator is not null && FunctionContainsTriggers("httpTrigger");

        using var requestActivity = telemetryClient.StartRequestOperation(hostActivity);

        var requestTelemetry = requestActivity.Telemetry;

        requestTelemetry.Name = context.FunctionDefinition.Name;
        requestTelemetry.Context.Operation.Name = context.FunctionDefinition.Name;

        if (shouldDelegateRequestActivity)
        {
            activityCoordinator!.StartRequestActivity(context.InvocationId, requestTelemetry, context.CancellationToken);
        }

        var success = false;

        try
        {
            await next(context);

            success = true;
        }
        finally
        {
            if (shouldDelegateRequestActivity)
            {
                await activityCoordinator!.WaitForRequestActivityCompletedAsync(context.InvocationId);
            }
            else
            {
                requestTelemetry.Success = success;
            }

            if (hostActivity.TryGetCorrelationContext() is ActivityContext activityContext)
            {
                requestTelemetry.Context.Operation.Id = activityContext.TraceId.ToString();
                requestTelemetry.Context.Operation.ParentId = activityContext.SpanId.ToString();
            }

            foreach (var property in hostActivity.Baggage)
            {
                requestTelemetry.Properties.TryAdd(property.Key, property.Value);
            }
        }

        bool FunctionContainsTriggers(params string[] expectedTriggers)
        {
            return this.functionTriggers
                .GetOrAdd(
                    context.FunctionId,
                    static (_, context) => [.. context.FunctionDefinition.InputBindings.Select(binding => binding.Value.Type)],
                    context)
                .Intersect(expectedTriggers, StringComparer.OrdinalIgnoreCase)
                .Any();
        }
    }
}
