## [ASP.NET Core middleware support](src/AzureFunctions.Worker.Extensions.AspNetCore/readme.md)
- Use ASP.NET Core built-in middlewares: UseAuthentication, UseAuthorization, or any other custom ones
- Use ASP.NET Core built-in bindings: FromQuery, FromBody, IFormFile, or any other custom ones
- Have full IActionResult/IResult integration as in MVC

## NuGet package
[https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.AspNetCore](https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.AspNetCore)

## 🔴Migration to v2 Azure Functions SDK🔴

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
            app.UseMiddleware<ExceptionHandlingMiddleware>(); // <-- custom ASP.NET middleware for exception handling of all HTTP triggered requests

            app.UseAuthentication();  // <-- built-in ASP.NET middleware
            app.UseAuthorization();  // <-- built-in ASP.NET middleware
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
    .ConfigureAspNetCoreMvcIntegration(mvcBuilder =>
    {
        // additional MVC builder customization
        mvcBuilder.AddMvcOptions(mvcOptions => { });
    })
    .UseAspNetCoreMiddleware(app =>
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>(); // <-- custom ASP.NET middleware for exception handling of all HTTP triggered requests
    
        app.UseAuthentication();  // <-- built-in ASP.NET middleware
        app.UseAuthorization();  // <-- built-in ASP.NET middleware
    });
```
