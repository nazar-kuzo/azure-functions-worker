using AzureFunctions.Worker.Extensions.ApplicationInsights;
using AzureFunctions.Worker.Extensions.TestHost.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var host = new HostBuilder()
    .ConfigureServices((hostingContext, services) =>
    {
        ConfigureLogging(hostingContext, services);

        ConfigureAuthentication(services);

        ConfigureAuthorization(services);

        ConfigureOptions(services);

        ConfigureSwagger(hostingContext, services);
    })
    .ConfigureFunctionsWebApplication(worker =>
    {
        worker.AddAspNetCoreIntegration();

        worker.UseAspNetCoreMiddleware(app =>
        {
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseFunctionSwaggerUI();
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
    services.Configure((Action<JsonSerializerOptions>) JsonOptionsConfigurator);
    services.Configure<JsonOptions>(jsonOptions =>
    {
        JsonOptionsConfigurator(jsonOptions.JsonSerializerOptions);
    });

    static void JsonOptionsConfigurator(JsonSerializerOptions jsonOptions)
    {
        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        jsonOptions.Converters.Add(new JsonStringEnumConverter());
    }
}

static void ConfigureSwagger(HostBuilderContext hostingContext, IServiceCollection services)
{
    services
        .AddSwaggerGen(swaggerOptions =>
        {
            foreach (var xmlFile in Directory.GetFiles(hostingContext.HostingEnvironment.ContentRootPath, "*.xml", SearchOption.TopDirectoryOnly))
            {
                swaggerOptions.IncludeXmlComments(xmlFile);
            }

            var securityDefinitions = new[]
            {
                new OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Description = "JWT Authorization header using the Bearer scheme.",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    Reference = new OpenApiReference
                    {
                        Id = "Bearer",
                        Type = ReferenceType.SecurityScheme,
                    },
                },
            };

            foreach (var definition in securityDefinitions)
            {
                swaggerOptions.AddSecurityDefinition(definition.Scheme, definition);
            }

            var documents = new (string DocumentName, OpenApiInfo DocumentInfo)[]
            {
                ("v1", new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "Client API",
                }),
                ("internal", new OpenApiInfo
                {
                    Version = "1.0.0",
                    Title = "Internal API",
                }),
            };

            foreach (var document in documents)
            {
                swaggerOptions.SwaggerDoc(document.DocumentName, document.DocumentInfo);
            }

            swaggerOptions.DocInclusionPredicate((documentName, action) => documentName == (action.GroupName ?? "v1"));
            swaggerOptions.DescribeAllParametersInCamelCase();
            swaggerOptions.EnableAnnotations(enableAnnotationsForInheritance: true, enableAnnotationsForPolymorphism: true);

            swaggerOptions.OperationFilter<SecurityRequirementOperationFilter>();
        });
}