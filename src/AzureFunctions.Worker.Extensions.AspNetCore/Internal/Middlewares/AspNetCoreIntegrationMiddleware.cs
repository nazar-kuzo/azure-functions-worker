using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal.Middlewares;

/// <summary>
/// Worker middleware that integrates AspNetCore middlewares into the execution pipeline
/// </summary>
/// <param name="httpContextAccessor">Http context accessor</param>
/// <param name="actionContextAccessor">Action context accessor</param>
/// <param name="actionResultTypeMapper">AspNetCore ActionResult type mapper</param>
/// <param name="metadataProvider">AspNetCore function metadata provider</param>
internal class AspNetCoreIntegrationMiddleware(
    IHttpContextAccessor httpContextAccessor,
    IActionContextAccessor actionContextAccessor,
    IActionResultTypeMapper actionResultTypeMapper,
    AspNetCoreFunctionMetadataProvider metadataProvider)
    : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext()!;

        var functionMetadata = metadataProvider.GetFunctionMetadata(context.FunctionDefinition.Name);

        // lets access FunctionContext from withing AspNetCore middleware
        httpContext.Features.Set(context);

        // TODO: find proper trace ID compatible with cross function execution
        // align HTTP trace ID with worker trace ID
        httpContext.TraceIdentifier = Activity.Current?.TraceId.ToString() ?? context.InvocationId;

        // enables IHttpContextAccessor support
        httpContextAccessor.HttpContext = httpContext;

        actionContextAccessor.ActionContext = new ActionContext
        {
            HttpContext = httpContext,
            ActionDescriptor = functionMetadata.ActionDescriptor,
            RouteData = httpContext.GetRouteData(),
        };

        await next(context);

        if (functionMetadata.HttpResultBinding is not null)
        {
            await TryExecuteHttpResult();
        }

        async Task TryExecuteHttpResult()
        {
            if (functionMetadata.HttpResultBinding.Name == "$return")
            {
                var invocationResult = context.GetInvocationResult();

                if (invocationResult.Value is null &&
                    functionMetadata.HttpResultDataType is null &&
                    !httpContext.Response.HasStarted)
                {
                    await new NoContentResult().ExecuteResultAsync(actionContextAccessor.ActionContext);

                    return;
                }

                if (invocationResult.Value is not null &&
                    invocationResult.Value is not HttpResponseData &&
                    invocationResult.Value is not IActionResult)
                {
                    invocationResult.Value = actionResultTypeMapper
                        .Convert(invocationResult.Value, functionMetadata.HttpResultDataType!);
                }

                // TODO: remove this code once https://github.com/Azure/azure-functions-dotnet-worker/issues/2682 is fixed
                if (invocationResult.Value is IActionResult actionResult)
                {
                    await actionResult.ExecuteResultAsync(actionContextAccessor.ActionContext);

                    invocationResult.Value = null;
                }
                else if (invocationResult.Value is IResult result)
                {
                    await result.ExecuteAsync(httpContext);

                    invocationResult.Value = null;
                }
            }
            else
            {
                var outputBindingData = context
                    .GetOutputBindings<object?>()
                    .FirstOrDefault(binding => binding.Name == functionMetadata.HttpResultBinding.Name);

                if (outputBindingData?.Value is not null &&
                    outputBindingData.Value is not HttpResponseData &&
                    outputBindingData.Value is not IActionResult)
                {
                    outputBindingData.Value = actionResultTypeMapper
                        .Convert(outputBindingData.Value, functionMetadata.HttpResultDataType!);
                }

                // TODO: remove this code once https://github.com/Azure/azure-functions-dotnet-worker/issues/2682 is fixed
                if (outputBindingData?.Value is IActionResult actionResult)
                {
                    await actionResult.ExecuteResultAsync(actionContextAccessor.ActionContext);

                    outputBindingData.Value = Enumerable.Empty<object>();
                }
                else if (outputBindingData?.Value is IResult result)
                {
                    await result.ExecuteAsync(httpContext);

                    outputBindingData.Value = Enumerable.Empty<object>();
                }
            }
        }
    }
}
