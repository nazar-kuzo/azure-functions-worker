namespace AzureFunctions.Worker.Extensions.TestHost.Models;

public sealed class QueueMessage<T>
{
    public string? TraceParent { get; set; }

    public string? TraceState { get; set; }

    public IEnumerable<KeyValuePair<string, string?>>? Baggage { get; set; }

    public required T Data { get; set; }
}
