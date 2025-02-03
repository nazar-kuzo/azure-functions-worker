using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.ApplicationInsights.Extensibility.Implementation;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

/// <summary>
/// Copy of the internal <see cref="Microsoft.ApplicationInsights.TelemetryClientExtensions.StartOperation{T}(TelemetryClient, Activity)"/>
/// in order to avoid throwing and catching exception during <see cref="Activity.Start"/> since it is already started.
/// </summary>
internal static partial class TelemetryClientExtensions
{
    private static readonly ConstructorInfo OperationHolderConstructor = typeof(OperationTelemetry).Assembly
        .GetType("Microsoft.ApplicationInsights.Extensibility.Implementation.OperationHolder`1")!
        .MakeGenericType(typeof(RequestTelemetry))
        .GetConstructors()[0];

    public static IOperationHolder<RequestTelemetry> StartRequestOperation(this TelemetryClient telemetryClient, Activity activity)
    {
        ArgumentNullException.ThrowIfNull(telemetryClient);
        ArgumentNullException.ThrowIfNull(activity);

        var originalActivity = default(Activity);

        // not started activity, default case
        if (activity.Id == null)
        {
            originalActivity = Activity.Current;
        }

        string? legacyRoot = null;
        string? legacyParent = null;

        if (Activity.DefaultIdFormat == ActivityIdFormat.W3C &&
            activity.ParentId != null &&
            !activity.ParentId.StartsWith("00-", StringComparison.Ordinal))
        {
            // save parent
            legacyParent = activity.ParentId;

            if (activity.RootId != null && GetTraceIdRegex().IsMatch(activity.RootId))
            {
                // reuse root id when compatible with trace ID
                activity = CopyFromCompatibleRoot(activity);
            }
            else
            {
                // or store legacy root in custom property
                legacyRoot = activity.RootId;
            }
        }

        // start activity if not started
        if (activity.Id == null)
        {
            activity.Start();
        }

        var requestTelemetry = ActivityToTelemetry(activity);

        if (legacyRoot != null)
        {
            requestTelemetry.Properties.Add("ai_legacyRootId", legacyRoot);
        }

        if (legacyParent != null)
        {
            requestTelemetry.Context.Operation.ParentId = legacyParent;
        }

        telemetryClient.Initialize(requestTelemetry);

        requestTelemetry.Start();

        return (IOperationHolder<RequestTelemetry>) OperationHolderConstructor.Invoke([telemetryClient, requestTelemetry, originalActivity]);
    }

    [GeneratedRegex("^[a-f0-9]{32}$", RegexOptions.Compiled)]
    private static partial Regex GetTraceIdRegex();

    private static RequestTelemetry ActivityToTelemetry(Activity activity)
    {
        var telemetry = new RequestTelemetry { Name = activity.OperationName };

        var operationContext = telemetry.Context.Operation;

        operationContext.Name = activity.GetOperationName();

        if (activity.IdFormat == ActivityIdFormat.W3C)
        {
            operationContext.Id = activity.TraceId.ToHexString();
            telemetry.Id = activity.SpanId.ToHexString();

            if (string.IsNullOrEmpty(operationContext.ParentId) && activity.ParentSpanId != default)
            {
                operationContext.ParentId = activity.ParentSpanId.ToHexString();
            }
        }
        else
        {
            operationContext.Id = activity.RootId;
            operationContext.ParentId = activity.ParentId;
            telemetry.Id = activity.Id;
        }

        foreach (var item in activity.Baggage)
        {
            if (!telemetry.Properties.ContainsKey(item.Key))
            {
                telemetry.Properties.Add(item);
            }
        }

        foreach (var item in activity.Tags)
        {
            if (!telemetry.Properties.ContainsKey(item.Key))
            {
                telemetry.Properties.Add(item);
            }
        }

        return telemetry;
    }

    private static string? GetOperationName(this Activity activity)
    {
        return activity.Tags.FirstOrDefault(tag => tag.Key == "OperationName").Value;
    }

    private static Activity CopyFromCompatibleRoot(Activity from)
    {
        var copy = new Activity(from.OperationName);

        copy.SetParentId(ActivityTraceId.CreateFromString(from.RootId.AsSpan()),  default, from.ActivityTraceFlags);

        foreach (var tag in from.Tags)
        {
            copy.AddTag(tag.Key, tag.Value);
        }

        foreach (var baggage in from.Baggage)
        {
            copy.AddBaggage(baggage.Key, baggage.Value);
        }

        copy.TraceStateString = from.TraceStateString;

        return copy;
    }
}
