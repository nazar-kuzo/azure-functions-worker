using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Context.Features;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal;

/// <summary>
/// Worker middleware that integrates AspNetCore middlewares into the execution pipeline
/// </summary>
/// <param name="httpContextAccessor">Http context accessor</param>
/// <param name="actionResultTypeMapper">AspNetCore ActionResult type mapper</param>
/// <param name="parameterBinder">AspNetCore parameter binder</param>
/// <param name="metadataProvider">AspNetCore function metadata provider</param>
internal class AspNetCoreIntegrationMiddleware(
    IHttpContextAccessor httpContextAccessor,
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
            // lets access FunctionContext from withing AspNetCore middleware
            httpContext.Features.Set(context);

            // enables IHttpContextAccessor support
            httpContextAccessor.HttpContext = httpContext;

            var functionMetadata = metadataProvider.GetFunctionMetadata(context.FunctionDefinition.Name);

            // extends built-in input binding feature with AspNetCore attributes binding support
            if (functionMetadata.AspNetCoreParameters.Length > 0 &&
                context.Features.Get<IFunctionInputBindingFeature>() is { } inputBindingFeature)
            {
                context.Features.Set<IFunctionInputBindingFeature>(
                    new AspNetCoreFunctionInputBindingFeature(parameterBinder, inputBindingFeature));
            }

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
        }
        else
        {
            await next(context);
        }
    }
}
