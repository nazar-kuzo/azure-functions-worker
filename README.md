# Azure Functions Worker (isolated) extensions

Boost your productivity with better ASP.NET Core framework integration like in old good MVC days.

## [Standalone Application Insights](src/AzureFunctions.Worker.Extensions.ApplicationInsights/readme.md)
- Completely opt-out worker host logging to Application insights
- Have control over `RequestTelemetry` item in your code
- Debloat worker host logs
- Keep Application insights metrics and perf counters
## [ASP.NET Core middleware support](src/AzureFunctions.Worker.Extensions.AspNetCore/readme.md)
- Use ASP.NET Core built-in middlewares: UseAuthentication, UseAuthorization, or any other custom ones
- Use ASP.NET Core built-in bindings: FromQuery, FromBody, IFormFile, or any other custom ones
- Have full IActionResult/IResult integration as in MVC
## [Swashbuckle API explorer for Azure Functions](src/AzureFunctions.Worker.Extensions.Swashbuckle/readme.md)
- Use Swashbuckle (Swagger UI) api explorer with zero midifications to your code
- Get full Swagger extensibility as in MVC

# ${\textsf{\color{red}Migration to v2 Azure Functions SDK}}$

There are some changes in the API for bootstrapping worker, therefore extensions should be registered differently,
please reffer to migration block for each extension package separately 
