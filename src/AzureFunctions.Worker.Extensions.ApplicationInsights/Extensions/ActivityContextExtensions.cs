namespace System.Diagnostics;

public static class ActivityContextExtensions
{
    private const string TraceParentKey = "correlation.traceParent";
    private const string TraceStateKey = "correlation.traceState";

    /// <summary>
    /// Applies a correlation context to the current Activity using a W3C trace parent header string.
    /// </summary>
    /// <param name="activity">The Activity to configure (usually Activity.Current).</param>
    /// <param name="traceParent">The W3C trace parent header (e.g., "00-{traceId}-{parentSpanId}-{flags}")</param>
    /// <param name="traceState">The trace state</param>
    /// <param name="baggage">The trace baggage</param>
    public static void ApplyCorrelationContext(
        this Activity activity,
        string? traceParent,
        string? traceState = null,
        IEnumerable<KeyValuePair<string, string?>>? baggage = null)
    {
        activity.SetTag(TraceParentKey, traceParent);
        activity.SetTag(TraceStateKey, traceState);

        foreach (var item in baggage ?? [])
        {
            activity.SetBaggage(item.Key, item.Value);
        }
    }

    /// <summary>
    /// Attempts to extract a W3C <see cref="ActivityContext"/> (trace context)
    /// from the current <see cref="Activity"/> using the trace parent and trace state tags.
    /// </summary>
    /// <param name="activity">The current <see cref="Activity"/> instance from which to retrieve the correlation context</param>
    /// <returns>A valid <see cref="ActivityContext"/> if the trace parent tag is present and successfully parsed; otherwise, null</returns>
    public static ActivityContext? TryGetCorrelationContext(this Activity activity)
    {
        var traceParent = (string?) activity.GetTagItem(TraceParentKey);
        var traceState = (string?) activity.GetTagItem(TraceStateKey);

        if (ActivityContext.TryParse(traceParent, traceState, out var context))
        {
            activity.SetTag(TraceParentKey, null);
            activity.SetTag(TraceStateKey, null);

            return context;
        }

        return null;
    }
}
