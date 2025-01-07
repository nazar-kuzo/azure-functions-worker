using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

/// <summary>
/// Maps HTTP request properties to <see cref="RequestTelemetry"/>
/// </summary>
/// <param name="activityCoordinator">Http activity coordinator</param>
internal class WorkerHttpRequestMappingMiddleware(
    HttpRequestActivityCoordinator activityCoordinator)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.Request.Headers["x-ms-invocation-id"].FirstOrDefault() is { } invocationId &&
            await activityCoordinator.WaitForRequestActivityStartedAsync(invocationId) is { } requestTelemetry)
        {
            requestTelemetry.Url = new Uri(context.Request.GetDisplayUrl());

            requestTelemetry.Properties["HTTP method"] = context.Request.Method;
            requestTelemetry.Properties["Request path"] = context.Request.Path;

            int responseCode = 0;

            try
            {
                await next(context);

                responseCode = context.Response.StatusCode;
            }
            catch
            {
                responseCode = StatusCodes.Status500InternalServerError;
            }
            finally
            {
                requestTelemetry.ResponseCode = responseCode.ToString();

                requestTelemetry.Success = responseCode >= 200 && responseCode <= 399;

                activityCoordinator.CompleteRequestActivity(invocationId);
            }
        }
        else
        {
            await next(context);
        }
    }
}
