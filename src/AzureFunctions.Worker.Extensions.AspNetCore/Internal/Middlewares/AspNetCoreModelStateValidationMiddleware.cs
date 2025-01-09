using AzureFunctions.Worker.Extensions.AspNetCore.Internal.ModelBinding;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal.Middlewares;

/// <summary>
/// Middleware that responds to invalid <see cref="ActionContext.ModelState"/>.
/// This middleware mimics <see cref="ModelStateInvalidFilter"/> in aspnet core.
/// </summary>
/// <param name="parameterBinder">AspNet core function parameter binder</param>
/// <param name="apiBehaviorOptions">Api behavior options</param>
/// <param name="actionContextAccessor">Action context accessor</param>
internal class AspNetCoreModelStateValidationMiddleware(
    AspNetCoreFunctionParameterBinder parameterBinder,
    IOptions<ApiBehaviorOptions> apiBehaviorOptions,
    IActionContextAccessor actionContextAccessor) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var functionContext = context.GetFunctionContext()!;

        await parameterBinder.BindAspNetCoreFunctionInputAsync(functionContext);

        if (!actionContextAccessor.ActionContext!.ModelState.IsValid)
        {
            await apiBehaviorOptions.Value
                .InvalidModelStateResponseFactory(actionContextAccessor.ActionContext)
                .ExecuteResultAsync(actionContextAccessor.ActionContext);
        }
        else
        {
            await next(context);
        }
    }
}
