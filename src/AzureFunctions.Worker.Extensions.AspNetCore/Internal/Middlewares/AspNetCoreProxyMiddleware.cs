using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;

namespace AzureFunctions.Worker.Extensions.AspNetCore.Internal.Middlewares;

/// <summary>
/// Worker middleware that integrates AspNetCore middlewares into the execution pipeline
/// </summary>
/// <param name="aspnetMiddleware">AspNetCore Application pipeline request delegate</param>
internal class AspNetCoreProxyMiddleware(RequestDelegate aspnetMiddleware) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var httpContext = context.GetHttpContext()!;

        // set worker middleware to be called at the end of AspNetCore pipeline
        httpContext.Items.Add("__WorkerMiddleware", () => next(context));

        await aspnetMiddleware.Invoke(httpContext);
    }
}
