using AzureFunctions.Worker.Extensions.AspNetCore;
using AzureFunctions.Worker.Extensions.AspNetCore.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Worker extensions to provide improved AspNetCore integration
/// </summary>
public static class WorkerExtensions
{
    /// <summary>
    /// Enables AspNetCore function parameter binding and exposes AspNetCore related Function metadata
    /// </summary>
    /// <param name="worker">FunctionsWorker ApplicationBuilder</param>
    /// <returns>Mvc builder</returns>
    public static IMvcCoreBuilder AddAspNetCoreIntegration(this IFunctionsWorkerApplicationBuilder worker)
    {
        worker.Services.AddHttpContextAccessor();

        worker.Services.AddTransient<IStartupFilter, StartupFilter>();

        worker.Services.TryAddSingleton<AspNetCoreFunctionMetadataProvider>();
        worker.Services.AddSingleton<AspNetCoreFunctionParameterBinder>();

        worker.UseMiddleware<AspNetCoreIntegrationMiddleware>();

        return worker.Services.AddMvcCore();
    }

    /// <summary>
    /// Provides ability to register AspNetCore Middleware as Worker Middleware.
    /// </summary>
    /// <remarks>
    /// AspNetCore Middleware has dependency on HttpContext so it will be executed only with Http-triggered functions.
    /// </remarks>
    /// <param name="worker">Functions worker Application builder</param>
    /// <param name="applicationBuilder">AspNetCore Application builder</param>
    public static void UseAspNetCoreMiddleware(
        this IFunctionsWorkerApplicationBuilder worker,
        Action<IApplicationBuilder> applicationBuilder)
    {
        worker.Services.TryAddSingleton<AspNetCoreFunctionMetadataProvider>();

        // register AspNetCore middlewares as singleton service since it depends on ServiceProvider
        worker.Services.AddSingleton(serviceProvider =>
        {
            var app = new ApplicationBuilder(serviceProvider);

            applicationBuilder(app);

            // register worker middleware as last middleware in AspNetCore execution pipeline
            app.Use((HttpContext httpContext, Func<Task> _) =>
            {
                httpContext.Items.Remove("__WorkerMiddleware", out var workerMiddleware);

                return ((Func<Task>) workerMiddleware!).Invoke();
            });

            return ActivatorUtilities.CreateInstance<AspNetCoreProxyMiddleware>(serviceProvider, app.Build());
        });

        // let internal UseEndpoints middleware to suppress unhandled authorization
        worker.Services.PostConfigure<RouteOptions>(routeOptions =>
        {
            routeOptions.SuppressCheckForUnhandledSecurityMetadata = true;
        });

        // register worker middleware that invokes AspNetCore middleware and passes next delegate
        worker.Use(next => context =>
        {
            var httpContext = context.GetHttpContext();

            if (httpContext == null)
            {
                return next(context);
            }
            else
            {
                var middleware = context.InstanceServices.GetRequiredService<AspNetCoreProxyMiddleware>();

                return middleware.Invoke(context, next);
            }
        });
    }
}
