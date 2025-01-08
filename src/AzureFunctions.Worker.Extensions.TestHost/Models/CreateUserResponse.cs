using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Worker.Extensions.TestHost.Models;

public class CreateUserResponse
{
    [HttpResult]
    public required UserInfo HttpResult { get; set; }

    [QueueOutput("user-created")]
    public required UserInfo QueueValue { get; set; }
}
