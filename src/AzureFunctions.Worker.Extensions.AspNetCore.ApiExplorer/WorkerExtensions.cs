using DotSwashbuckle.AspNetCore.Swagger;
using DotSwashbuckle.AspNetCore.SwaggerGen;
using DotSwashbuckle.AspNetCore.SwaggerUI;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Worker extensions to provide SwaggerUI middleware
/// </summary>
public static class WorkerExtensions
{
    private static readonly string RoutePrefix = "/api/swagger";
    private static readonly string IndexDocumentPath = $"/{RoutePrefix}/index.html";

    /// <summary>
    /// Registers the SwaggerUI middleware with optional setup action for DI-injected options
    /// </summary>
    /// <param name="app">IApplicationBuilder</param>
    /// <param name="swaggerOptionsSetup">SwaggerOptions configurator</param>
    /// <param name="uiOptionsSetup">SwaggerUIOptions configurator</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder UseFunctionSwaggerUI(
        this IApplicationBuilder app,
        Action<SwaggerOptions>? swaggerOptionsSetup = null,
        Action<SwaggerUIOptions>? uiOptionsSetup = null)
    {
        var swaggerGenOptions = app.ApplicationServices.GetRequiredService<IOptions<SwaggerGenOptions>>();

        app.Use(next => httpContext =>
        {
            // static content wont serve if HttpContext has endpoint set
            if (httpContext.Request.Path.Value is string path &&
                path.StartsWith(RoutePrefix) &&
                path != IndexDocumentPath)
            {
                httpContext.SetEndpoint(null);
            }

            return next(httpContext);
        });

        app.UseSwagger(swaggerOptions =>
        {
            swaggerOptionsSetup?.Invoke(swaggerOptions);

            swaggerOptions.RouteTemplate = $"{RoutePrefix}/{{documentName}}/swagger.json";
        });

        app.UseSwaggerUI(uiOptions =>
        {
            uiOptionsSetup?.Invoke(uiOptions);

            uiOptions.RoutePrefix = RoutePrefix.TrimStart('/');

            // create default document endpoints based on SwaggerGenerator options
            if (uiOptions.ConfigObject.Urls?.Any() != true)
            {
                foreach (var document in swaggerGenOptions.Value.SwaggerGeneratorOptions.SwaggerDocs)
                {
                    uiOptions.SwaggerEndpoint($"{RoutePrefix}/{document.Key}/swagger.json", document.Value.Title);
                }
            }
        });

        return app;
    }
}
