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
        var httpContext = context.GetHttpContext();

        if (httpContext == null)
        {
            await next(context);

            return;
        }

        var functionMetadata = metadataProvider.GetFunctionMetadata(context.FunctionDefinition.Name);

        // lets access FunctionContext from withing AspNetCore middleware
        httpContext.Features.Set(context);

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
            TryConvertHttpResult();
        }

        void TryConvertHttpResult()
        {
            if (functionMetadata.HttpResultBinding.Name == "$return")
            {
                var invocationResult = context.GetInvocationResult();

                if (invocationResult.Value is not null &&
                    invocationResult.Value is not HttpResponseData &&
                    invocationResult.Value is not IActionResult)
                {
                    invocationResult.Value = actionResultTypeMapper
                        .Convert(invocationResult.Value, functionMetadata.HttpResultDataType!);
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
            }
        }
    }
}
