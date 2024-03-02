using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Worker.Extensions.TestHost.Functions;

[ApiExplorerSettings(GroupName = "internal")]
public class Application
{
    [Function($"{nameof(Application)}-{nameof(UpdateDatabase)}")]
    public void UpdateDatabase(
        [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "application/update-database")] HttpRequest request)
    {
        // method intentionally left empty.
    }
}
