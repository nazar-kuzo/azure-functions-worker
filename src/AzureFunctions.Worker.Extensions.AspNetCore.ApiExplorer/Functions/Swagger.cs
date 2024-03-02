using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Worker.Extensions.AspNetCore.ApiExplorer.Functions;

/// <summary>
/// Swagger UI backing functions
/// </summary>
[ApiExplorerSettings(IgnoreApi = true)]
public class Swagger
{
    /// <summary>
    /// Swagger UI backing function
    /// </summary>
    /// <param name="request">Http Request</param>
    [Function(nameof(SwaggerUI))]
    public void SwaggerUI([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "swagger/{*path}")] HttpRequest request)
    {
        throw new InvalidOperationException("This method should never be executed due to Swagger middleware");
    }
}
