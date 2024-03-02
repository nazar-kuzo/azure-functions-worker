using System.Text;
using DotSwashbuckle.AspNetCore.SwaggerGen;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;

namespace AzureFunctions.Worker.Extensions.TestHost.Swagger;

/// <summary>
/// Adds security requirements to API operations based on [Authorize] attributes
/// </summary>
/// <param name="swaggerOptions">Swagger Options</param>
internal class SecurityRequirementOperationFilter(IOptions<SwaggerGenOptions> swaggerOptions) : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var authorizeAttributes = context
            .ApiDescription
            .CustomAttributes()
            .OfType<IAuthorizeData>()
            .ToList();

        var hasAnonymousAttribute = context
            .ApiDescription
            .CustomAttributes()
            .OfType<AllowAnonymousAttribute>()
            .Any();

        if (!hasAnonymousAttribute && authorizeAttributes.Count > 0)
        {
            foreach (var scheme in swaggerOptions.Value.SwaggerGeneratorOptions.SecuritySchemes.Values)
            {
                operation.Security.Add(new OpenApiSecurityRequirement
                {
                    { scheme, new List<string>() },
                });
            }

            var policies = authorizeAttributes
                .Where(authData => !string.IsNullOrEmpty(authData.Policy))
                .Select(authData => authData.Policy)
                .ToList();

            var roles = authorizeAttributes
                .Where(authData => !string.IsNullOrEmpty(authData.Roles))
                .SelectMany(authData => authData.Roles!.Split(",", StringSplitOptions.RemoveEmptyEntries))
                .ToList();

            var stringBuilder = new StringBuilder(operation.Description ?? string.Empty);

            if (roles.Count > 0)
            {
                stringBuilder.AppendLine("Authorized roles:");

                foreach (var role in roles)
                {
                    stringBuilder.Append($"* `{role}`\n\n");
                }
            }

            if (policies.Count > 0)
            {
                stringBuilder.AppendLine("Authorized policies:");

                foreach (var policy in policies)
                {
                    stringBuilder.Append($"* `{policy}`\n\n");
                }
            }

            operation.Description = stringBuilder.ToString();
        }
    }
}
