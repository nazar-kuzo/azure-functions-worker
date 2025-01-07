using AzureFunctions.Worker.Extensions.ApplicationInsights;
using AzureFunctions.Worker.Extensions.TestHost.Swagger;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

var builder = FunctionsApplication.CreateBuilder(args);

// should be called before "ConfigureFunctionsWebApplication"
builder.ConfigureStandaloneApplicationInsights();

// should be used for HTTP triggered APIs
builder.ConfigureFunctionsWebApplication();

// configures AspNetCore input bindings
builder.ConfigureAspNetCoreIntegration();

ConfigureAuthentication();
ConfigureAuthorization();
ConfigureOptions();
ConfigureSwagger();

builder.UseAspNetCoreMiddleware(app =>
{
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseFunctionSwaggerUI(
        uiOptionsSetup: uiOptions =>
        {
            uiOptions.DocumentTitle = "Worker Extensions";
        },
        faviconFileName: "icon.png");
});

#if DEBUG

builder.Configuration
    .AddJsonFile("local.settings.json", optional: false, reloadOnChange: true)
    .AddUserSecrets<Program>();

#endif

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
                authorization == "Bearer 123456";
        })
        .Build();

    builder.Services
        .AddAuthorizationBuilder()
        .SetInvokeHandlersAfterFailure(false)
        .SetDefaultPolicy(defaultPolicy);
}

void ConfigureOptions()
{
    builder.Services.Configure((Action<JsonSerializerOptions>) JsonOptionsConfigurator);
    builder.Services.Configure<JsonOptions>(jsonOptions =>
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