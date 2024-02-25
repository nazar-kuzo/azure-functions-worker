namespace AzureFunctions.Worker.Extensions.TestHost.Models;

public class UserInfo
{
    public Guid Id { get; set; }

    public required string Name { get; set; }

    public required string Email { get; set; }
}

public enum UserRole
{
    User,

    Admin,
}