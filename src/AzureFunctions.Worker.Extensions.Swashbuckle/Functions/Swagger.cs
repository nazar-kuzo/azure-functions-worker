using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Worker.Extensions.Swashbuckle.Functions;

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
    /// <returns>Not found result</returns>
    [Function(nameof(SwaggerUI))]
    public IActionResult SwaggerUI([HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "swagger/{*path}")] HttpRequest request)
    {
        return new NotFoundResult();
    }
}
