using System.Net.Mime;
using AzureFunctions.Worker.Extensions.ApplicationInsights;
using AzureFunctions.Worker.Extensions.TestHost.ExceptionHandling;
using AzureFunctions.Worker.Extensions.TestHost.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi;

var builder = FunctionsApplication.CreateBuilder(args);

// should be called before "ConfigureFunctionsWebApplication"
builder.ConfigureStandaloneApplicationInsights();

// connects to Azure App Configuration and caches settings locally
builder.LoadAppConfiguration();

// should be used for HTTP triggered APIs
builder.ConfigureFunctionsWebApplication();

// should be called after "ConfigureFunctionsWebApplication"
builder
    .ConfigureAspNetCoreMvcIntegration(mvcBuilder =>
    {
        mvcBuilder.AddMvcOptions(mvcOptions =>
        {
            if (mvcOptions.InputFormatters.OfType<SystemTextJsonInputFormatter>().FirstOrDefault() is { } jsonInputFormatter)
            {
                jsonInputFormatter.SupportedMediaTypes.Clear();
                jsonInputFormatter.SupportedMediaTypes.Add(MediaTypeNames.Application.Json);
            }

            mvcOptions.OutputFormatters.RemoveType<StringOutputFormatter>();

            if (mvcOptions.OutputFormatters.OfType<SystemTextJsonOutputFormatter>().FirstOrDefault() is { } jsonOutputFormatter)
            {
                jsonOutputFormatter.SupportedMediaTypes.Remove("text/json");
            }
        });
    });

builder.ConfigureDurableTaskClient();

builder.UseAspNetCoreMiddleware(app =>
{
    app.UseExceptionHandler();

    app.UseAuthentication();
    app.UseAuthorization();
    app.UseFunctionSwaggerUI(
        uiOptionsSetup: uiOptions =>
        {
            uiOptions.DocumentTitle = "Worker Extensions";
        },
        faviconFileName: "icon.png");
});

ConfigureAuthentication();
ConfigureAuthorization();
ConfigureOptions();
ConfigureExceptionHandling();
ConfigureInjectables();
ConfigureSwagger();

await builder.Build().RunAsync();

void ConfigureAuthentication()
{
    builder.Services
        .AddAuthentication(authenticationOptions =>
        {
            authenticationOptions.DefaultScheme = "Bearer";
            authenticationOptions.DefaultChallengeScheme = "Bearer";
            authenticationOptions.DefaultAuthenticateScheme = "Bearer";
        })
        .AddBearerToken("Bearer", tokenOptions => { });
}

void ConfigureAuthorization()
{
    var defaultPolicy = new AuthorizationPolicyBuilder()
        .RequireAssertion(context =>
        {
            return context.Resource is HttpContext httpContext &&
                httpContext.Request.Headers.TryGetValue("Authorization", out var authorization) &&
                authorization.ToString().StartsWith("Bearer");
        })
        .Build();

    builder.Services
        .AddAuthorizationBuilder()
        .SetInvokeHandlersAfterFailure(false)
        .SetDefaultPolicy(defaultPolicy);
}

void ConfigureOptions()
{
    if (builder.Environment.IsDevelopment())
    {
        builder.Configuration
            .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
            .AddUserSecrets<Program>();
    }

    builder.Services.Configure<JsonSerializerOptions>(JsonOptionsConfigurator);
    builder.Services.Configure<JsonOptions>(jsonOptions =>
    {
        JsonOptionsConfigurator(jsonOptions.JsonSerializerOptions);
    });

    static void JsonOptionsConfigurator(JsonSerializerOptions jsonOptions)
    {
        jsonOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        jsonOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        jsonOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    }
}

void ConfigureExceptionHandling()
{
    builder.Services
        .AddProblemDetails(problemDetailsOptions =>
        {
            problemDetailsOptions.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        })
        .AddExceptionHandler<GlobalExceptionHandler>();
}

void ConfigureInjectables()
{
    builder.Services
        .AddAzureTableCache(builder.Configuration, cacheOptions => cacheOptions.ApplicationName = "AzureFunctions.Worker.Extensions.TestHost");
}

void ConfigureSwagger()
{
    builder.Services
        .AddSwaggerGen(swaggerOptions =>
        {
            foreach (var xmlFile in Directory.GetFiles(AppContext.BaseDirectory, "*.xml", SearchOption.TopDirectoryOnly))
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
                    Title = "Test API",
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