using System.Net.Mime;
using System.Reflection;
using DotSwashbuckle.AspNetCore.Swagger;
using DotSwashbuckle.AspNetCore.SwaggerGen;
using DotSwashbuckle.AspNetCore.SwaggerUI;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;

namespace Microsoft.AspNetCore.Builder;

/// <summary>
/// Worker extensions to provide SwaggerUI middleware
/// </summary>
public static class WorkerExtensions
{
    private static readonly string RoutePrefix = "/api/swagger";
    private static readonly string FaviconPath = $"{RoutePrefix}/favicon-";
    private static readonly FileExtensionContentTypeProvider ContentTypeProvider = new();

    /// <summary>
    /// Registers the SwaggerUI middleware with optional setup action for DI-injected options
    /// </summary>
    /// <param name="app">IApplicationBuilder</param>
    /// <param name="swaggerOptionsSetup">SwaggerOptions configurator</param>
    /// <param name="uiOptionsSetup">SwaggerUIOptions configurator</param>
    /// <param name="faviconFileName">Custom favicon embedded file name</param>
    /// <returns>Application builder</returns>
    public static IApplicationBuilder UseFunctionSwaggerUI(
        this IApplicationBuilder app,
        Action<SwaggerOptions>? swaggerOptionsSetup = null,
        Action<SwaggerUIOptions>? uiOptionsSetup = null,
        string? faviconFileName = null)
    {
        var swaggerGenOptions = app.ApplicationServices.GetRequiredService<IOptions<SwaggerGenOptions>>();

        app.Use(next => httpContext =>
        {
            if (httpContext.Request.Path.Value is string path && path.StartsWith(RoutePrefix))
            {
                // static content wont serve if HttpContext has endpoint set
                httpContext.SetEndpoint(null);

                if (!string.IsNullOrEmpty(faviconFileName) &&
                    path.StartsWith(FaviconPath) &&
                    httpContext.GetFunctionContext() is { } functionContext &&
                    ReadEmbeddedFile(faviconFileName) is { } favicon)
                {
                    ContentTypeProvider.TryGetContentType(faviconFileName, out var contentType);

                    contentType ??= MediaTypeNames.Application.Octet;

                    functionContext.ReplyWithActionResult(new FileStreamResult(favicon.CreateReadStream(), contentType)
                    {
                        FileDownloadName = faviconFileName,
                    });

                    return Task.CompletedTask;
                }
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

    private static IFileInfo? ReadEmbeddedFile(string faviconFileName)
    {
        return new EmbeddedFileProvider(Assembly.GetEntryAssembly()!)
            .GetDirectoryContents(string.Empty)
            .SingleOrDefault(fileInfo => fileInfo.Name.EndsWith(faviconFileName));
    }
}
