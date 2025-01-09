using AzureFunctions.Worker.Extensions.AspNetCore;
using AzureFunctions.Worker.Extensions.AspNetCore.Internal;
using AzureFunctions.Worker.Extensions.AspNetCore.Internal.Middlewares;
using AzureFunctions.Worker.Extensions.AspNetCore.Internal.ModelBinding;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Routing;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Worker extensions to provide improved AspNetCore integration
/// </summary>
public static class WorkerExtensions
{
    /// <summary>
    /// Enables AspNetCore MVC model binding for Azure functions
    /// parameter binding and exposes AspNetCore related Function metadata
    /// </summary>
    /// <param name="worker">FunctionsWorker ApplicationBuilder</param>
    /// <returns>Mvc builder</returns>
    public static IMvcCoreBuilder ConfigureAspNetCoreMvcIntegration(this FunctionsApplicationBuilder worker)
    {
        if (!worker.Services.Any(descriptor => descriptor.ImplementationType?.FullName == "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.DefaultHttpCoordinator"))
        {
            throw new InvalidOperationException($"This method requires the \"ConfigureFunctionsWebApplication\" method to be called first");
        }

        worker.Services.AddHttpContextAccessor();

        worker.Services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
        worker.Services.TryAddSingleton<AspNetCoreFunctionMetadataProvider>();
        worker.Services.AddSingleton<AspNetCoreFunctionParameterBinder>();
        worker.Services.AddSingleton<IActionDescriptorProvider, FunctionActionDescriptorProvider>();
        worker.Services.AddTransient<IApplicationModelProvider, FunctionApplicationModelProvider>();
        worker.Services.AddTransient<IStartupFilter, WorkerStartupFilter>();
        worker.Services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, FunctionApiDescriptionProvider>());

        worker.UseWhen<AspNetCoreIntegrationMiddleware>(context => context.GetHttpContext() is not null);

        return worker.Services
            .AddMvcCore()
            .AddApiExplorer();
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
        worker.Services.TryAddSingleton<AspNetCoreModelStateValidationMiddleware>();

        worker.UseWhen<AspNetCoreProxyMiddleware>(context => context.GetHttpContext() is not null);

        // register AspNetCore middlewares as singleton service since it depends on ServiceProvider
        worker.Services.AddSingleton(serviceProvider =>
        {
            var app = new ApplicationBuilder(serviceProvider);

            applicationBuilder(app);

            // validate model state at the end of aspnet core pipeline
            app.Use((HttpContext httpContext, RequestDelegate next) => httpContext.RequestServices
                .GetRequiredService<AspNetCoreModelStateValidationMiddleware>()
                .InvokeAsync(httpContext, next));

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
    }
}
