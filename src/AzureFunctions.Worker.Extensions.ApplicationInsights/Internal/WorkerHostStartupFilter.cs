﻿using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AzureFunctions.Worker.Extensions.ApplicationInsights.Internal;

/// <summary>
/// Registers application middleware responsible to initialize <see cref="RequestTelemetry"/>
/// during function HTTP triggered execution
/// </summary>
internal class WorkerHostStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return builder =>
        {
            var activityCoordinator = builder.ApplicationServices.GetRequiredService<HttpActivityCoordinator>();

            builder.Use(next => async httpContext =>
            {
                if (httpContext.Request.Headers["x-ms-invocation-id"].FirstOrDefault() is { } invocationId &&
                    await activityCoordinator.WaitForRequestActivityStartedAsync(invocationId) is { } requestTelemetry)
                {
                    requestTelemetry.Url = new Uri(httpContext.Request.GetDisplayUrl());

                    requestTelemetry.Properties["HTTP method"] = httpContext.Request.Method;
                    requestTelemetry.Properties["Request path"] = httpContext.Request.Path;

                    int responseCode = 0;

                    try
                    {
                        await next(httpContext);

                        responseCode = httpContext.Response.StatusCode;
                    }
                    catch
                    {
                        responseCode = StatusCodes.Status500InternalServerError;
                    }
                    finally
                    {
                        requestTelemetry.ResponseCode = responseCode.ToString();

                        requestTelemetry.Success = responseCode >= 200 && responseCode <= 399;

                        activityCoordinator.StopRequestActivity(invocationId);
                    }
                }
                else
                {
                    await next(httpContext);
                }
            });

            next(builder);
        };
    }
}