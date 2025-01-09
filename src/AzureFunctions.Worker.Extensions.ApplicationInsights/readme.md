## [Standalone Application Insights](src/AzureFunctions.Worker.Extensions.ApplicationInsights/readme.md)
- Completely opt-out worker host logging to Application insights
- Have control over `RequestTelemetry` item in your code
- Debloat worker host logs
- Keep Application insights metrics and perf counters

## NuGet package
[https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.ApplicationInsights](https://www.nuget.org/packages/AzureFunctions.Worker.Extensions.ApplicationInsights)

## ${\textsf{\color{red}Migration to v2 Azure Functions SDK}}$

Microsoft suggest to use `FunctionsApplicationBuilder` over generic `HostBuilder` that is why v2 package will only support this api.

v1
```csharp
var host = new HostBuilder()
    .ConfigureServices((hostingContext, services) =>
    {
        services
            .AddApplicationInsightsTelemetryWorkerService()
            .ConfigureStandaloneFunctionsApplicationInsights(hostingContext.Configuration); // <-- magic happens here
    })
    .ConfigureFunctionsWebApplication(worker =>
    {
        // additional worker configuration
    })
    .Build();
```

v2
```csharp
var builder = FunctionsApplication.CreateBuilder(args);

// should be called before "ConfigureFunctionsWebApplication"
builder.ConfigureStandaloneApplicationInsights(); // <-- now magic happens here

// should be used for HTTP triggered APIs
builder.ConfigureFunctionsWebApplication();

// additional worker configuration
```
