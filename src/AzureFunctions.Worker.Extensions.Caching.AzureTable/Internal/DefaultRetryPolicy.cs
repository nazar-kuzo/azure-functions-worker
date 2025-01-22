using Azure;
using Azure.Core;
using Azure.Core.Pipeline;

namespace AzureFunctions.Worker.Extensions.Caching.AzureTable.Internal;

/// <summary>
/// Registers global error response handler which helps to
/// create cache table if `TableNotFound` error response received.
/// </summary>
internal sealed class DefaultRetryPolicy : RetryPolicy
{
    public Dictionary<string, Func<Task>> ErrorHandlers { get; } = [];

    protected override async ValueTask<bool> ShouldRetryAsync(HttpMessage message, Exception? exception)
    {
        exception ??= new RequestFailedException(message.Response);

        if (exception is RequestFailedException requestException &&
            this.ErrorHandlers.TryGetValue(requestException.ErrorCode!, out var handler))
        {
            await handler();

            return true;
        }

        return await base.ShouldRetryAsync(message, exception);
    }
}
