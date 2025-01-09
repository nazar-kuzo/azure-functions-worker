## [Swashbuckle API explorer for Azure Functions](src/AzureFunctions.Worker.Extensions.Swashbuckle/readme.md)
- Use Swashbuckle (Swagger UI) api explorer with zero midifications to your code
- Get full Swagger extensibility as in MVC

## NuGet package
v1
[https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.AspNetCore.ApiExplorer](https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.AspNetCore.ApiExplorer)

v2
[https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.Swashbuckle](https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.Swashbuckle)

## ${\textsf{\color{red}Migration to v2 Azure Functions SDK}}$

Microsoft suggest to use `FunctionsApplicationBuilder` over generic `HostBuilder` that is why v2 package will only support this api.

v1
```csharp
var host = new HostBuilder()
    .ConfigureServices((hostingContext, services) =>
    {
        // additional worker services configuration
    })
    .ConfigureFunctionsWebApplication(worker =>
    {
        worker
            .AddAspNetCoreIntegration()
            .AddMvcOptions(mvcOptions =>
            {
                // additional MVC options customization
            });

        worker.UseAspNetCoreMiddleware(app =>
        {
            app.UseFunctionSwaggerUI();  // <--- magic happens here
        });
    })
    .Build();
```

v2
```csharp
var builder = FunctionsApplication.CreateBuilder(args);

// should be used for HTTP triggered APIs
builder.ConfigureFunctionsWebApplication();

// should be called after "ConfigureFunctionsWebApplication"
builder
    .ConfigureAspNetCoreMvcIntegration()
    .AddMvcOptions(mvcOptions =>
    {
        // additional MVC options customization
    });

builder.UseAspNetCoreMiddleware(app =>
{
    app.UseFunctionSwaggerUI();  // <--- magic happens here
});
```
