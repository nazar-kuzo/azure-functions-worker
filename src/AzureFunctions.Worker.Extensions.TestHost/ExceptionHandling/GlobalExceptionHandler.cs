using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AzureFunctions.Worker.Extensions.TestHost.ExceptionHandling;

public class GlobalExceptionHandler(IProblemDetailsWriter problemDetailsWriter) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var httpResponseException = exception as HttpResponseException ?? new ServerException(string.Empty);

        var problemDetails = new ProblemDetails
        {
            Status = httpResponseException.StatusCode,
            Title = httpResponseException.StatusCode >= StatusCodes.Status500InternalServerError
                ? "Internal server error"
                : httpResponseException.Message,
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await problemDetailsWriter.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
            AdditionalMetadata = httpContext.GetEndpoint()?.Metadata,
        });

        return true;
    }
}
