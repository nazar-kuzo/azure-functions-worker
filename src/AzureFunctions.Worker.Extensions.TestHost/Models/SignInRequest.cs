namespace AzureFunctions.Worker.Extensions.TestHost.Models;

public class SignInRequest
{
    [Required(AllowEmptyStrings = false)]
    public required string Email { get; set; }

    [Required(AllowEmptyStrings = false)]
    public required string Password { get; set; }
}
