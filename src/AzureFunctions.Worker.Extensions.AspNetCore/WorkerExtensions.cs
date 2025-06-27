using AzureFunctions.Worker.Extensions.AspNetCore;
using AzureFunctions.Worker.Extensions.AspNetCore.Internal;
using AzureFunctions.Worker.Extensions.AspNetCore.Internal.Middlewares;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Filters;
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
    /// <param name="configureMvc">Optionally configure Mvc builder</param>
    /// <returns>Functions application builder</returns>
    public static FunctionsApplicationBuilder ConfigureAspNetCoreMvcIntegration(
        this FunctionsApplicationBuilder worker,
        Action<IMvcCoreBuilder>? configureMvc = null)
    {
        if (!worker.Services.Any(descriptor => descriptor.ImplementationType?.FullName == "Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.DefaultHttpCoordinator"))
        {
            throw new InvalidOperationException($"This method requires the \"ConfigureFunctionsWebApplication\" method to be called first");
        }

        worker.Services.AddHttpContextAccessor();

        worker.Services.TryAddSingleton<IActionContextAccessor, ActionContextAccessor>();
        worker.Services.TryAddSingleton<AspNetCoreFunctionMetadataProvider>();
        worker.Services.AddSingleton<IActionDescriptorProvider, FunctionActionDescriptorProvider>();
        worker.Services.AddTransient<IApplicationModelProvider, FunctionApplicationModelProvider>();
        worker.Services.AddTransient<IStartupFilter, WorkerStartupFilter>();
        worker.Services.TryAddEnumerable(ServiceDescriptor.Transient<IApiDescriptionProvider, FunctionApiDescriptionProvider>());

        worker.Services.AddScoped<IFilterMetadata>(serviceProvider =>
        {
            return new ModelStateInvalidFilter(
                serviceProvider.GetRequiredService<IOptions<ApiBehaviorOptions>>().Value,
                serviceProvider.GetRequiredService<ILogger<ModelStateInvalidFilter>>());
        });

        worker.UseWhen<AspNetCoreIntegrationMiddleware>(context => context.GetHttpContext() is not null);

        var mvcBuilder = worker.Services
            .AddMvcCore()
            .AddApiExplorer();

        configureMvc?.Invoke(mvcBuilder);

        return worker;
    }

    /// <summary>
    /// Configures custom action filter that validates the ModelState before function execution
    /// </summary>
    /// <typeparam name="T">Action filter type</typeparam>
    /// <param name="worker">FunctionsWorker ApplicationBuilder</param>
    /// <returns>Functions application builder</returns>
    public static FunctionsApplicationBuilder ConfigureModelStateInvalidFilter<T>(
        this FunctionsApplicationBuilder worker)
        where T : class, IFilterMetadata
    {
        worker.Services.AddScoped<IFilterMetadata, T>();

        return worker;
    }

    /// <summary>
    /// Provides ability to register AspNetCore Middleware as Worker Middleware.
    /// </summary>
    /// <remarks>
    /// AspNetCore Middleware has dependency on HttpContext so it will be executed only with Http-triggered functions.
    /// </remarks>
    /// <param name="worker">Functions worker Application builder</param>
    /// <param name="applicationBuilder">AspNetCore Application builder</param>
    /// <returns>Functions application builder</returns>
    public static FunctionsApplicationBuilder UseAspNetCoreMiddleware(
        this FunctionsApplicationBuilder worker,
        Action<IApplicationBuilder> applicationBuilder)
    {
        worker.Services.TryAddSingleton<AspNetCoreFunctionMetadataProvider>();
        worker.Services.TryAddSingleton<AspNetCoreModelStateValidationMiddleware>();

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

        worker.Use(next =>
        {
            return functionContext =>
            {
                if (functionContext.GetHttpContext() is not null)
                {
                    return functionContext.InstanceServices
                        .GetRequiredService<AspNetCoreProxyMiddleware>()
                        .Invoke(functionContext, next);
                }

                return next.Invoke(functionContext);
            };
        });

        return worker;
    }
}
