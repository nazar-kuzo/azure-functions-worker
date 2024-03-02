namespace AzureFunctions.Worker.Extensions.TestHost.Models;

/// <summary>
/// User info
/// </summary>
public class UserInfo
{
    /// <summary>
    /// User ID
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// User name
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// User email
    /// </summary>
    public required string Email { get; set; }
}