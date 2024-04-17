using AzureFunctions.Worker.Extensions.AspNetCore.Internal.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal.Middlewares;

/// <summary>
/// Worker middleware that integrates AspNetCore middlewares into the execution pipeline
/// </summary>
/// <param name="httpContextAccessor">Http context accessor</param>
/// <param name="actionContextAccessor">Action context accessor</param>
/// <param name="actionResultTypeMapper">AspNetCore ActionResult type mapper</param>
/// <param name="parameterBinder">AspNetCore parameter binder</param>
/// <param name="metadataProvider">AspNetCore function metadata provider</param>
internal class AspNetCoreIntegrationMiddleware(
    IHttpContextAccessor httpContextAccessor,
    IActionContextAccessor actionContextAccessor,
    IActionResultTypeMapper actionResultTypeMapper,
    AspNetCoreFunctionParameterBinder parameterBinder,
    AspNetCoreFunctionMetadataProvider metadataProvider)
    : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext();

        if (httpContext != null)
        {
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

            var invocationResult = context.GetInvocationResult();

            // try to convert raw object response to AspNetCore ObjectResult response
            if (invocationResult.Value is not null &&
                invocationResult.Value is not HttpResponseData &&
                invocationResult.Value is not IActionResult)
            {
                invocationResult.Value = actionResultTypeMapper.Convert(
                    invocationResult.Value,
                    functionMetadata.ReturnDataType);
            }

            if (invocationResult.Value is IActionResult actionResult)
            {
                await actionResult.ExecuteResultAsync(actionContextAccessor.ActionContext);

                // there's no need to return this result as no additional processing is required
                invocationResult.Value = null;
            }
        }
        else
        {
            await next(context);
        }
    }
}
