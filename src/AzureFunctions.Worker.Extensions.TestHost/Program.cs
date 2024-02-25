using AzureFunctions.Worker.Extensions.ApplicationInsights;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureServices((hostingContext, services) =>
    {
        ConfigureLogging(hostingContext, services);

        ConfigureAuthentication(services);

        ConfigureAuthorization(services);

        ConfigureOptions(services);
    })
    .ConfigureFunctionsWebApplication(worker =>
    {
        ConfigureAspNetCoreIntegration(worker);

        worker.UseAspNetCoreMiddleware(app =>
        {
            app.UseAuthentication();
            app.UseAuthorization();
        });
    })
#if DEBUG
    .ConfigureAppConfiguration(builder => builder
        .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
        .AddUserSecrets<Program>())
#endif
    .Build();

host.Run();

static void ConfigureLogging(HostBuilderContext hostingContext, IServiceCollection services)
{
    services
        .AddApplicationInsightsTelemetryWorkerService(appInsightsOptions =>
        {
            appInsightsOptions.ConnectionString = hostingContext.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];
        })
        .ConfigureStandaloneFunctionsApplicationInsights(hostingContext.Configuration);
}

static void ConfigureAuthentication(IServiceCollection services)
{
    services.AddAuthentication();
}

static void ConfigureAuthorization(IServiceCollection services)
{
    var defaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(context => true)
        .Build();

    services
        .AddAuthorizationBuilder()
        .SetInvokeHandlersAfterFailure(false)
        .SetDefaultPolicy(defaultPolicy);
}

static void ConfigureOptions(IServiceCollection services)
{
    services.Configure<JsonSerializerOptions>(options =>
    {
        options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        options.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    });
}

static void ConfigureAspNetCoreIntegration(IFunctionsWorkerApplicationBuilder worker)
{
    worker
        .AddAspNetCoreIntegration()
        .AddJsonOptions(jsonOptions =>
        {
            jsonOptions.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            jsonOptions.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            jsonOptions.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });
}