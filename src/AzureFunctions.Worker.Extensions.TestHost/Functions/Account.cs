using System.Diagnostics;
using AzureFunctions.Worker.Extensions.TestHost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Worker.Extensions.TestHost.Functions;

/// <summary>
/// Account API
/// </summary>
/// <param name="logger">Logger</param>
[Authorize]
public class Account(ILogger<Account> logger)
{
    /// <summary>
    /// GetUsers
    /// </summary>
    /// <param name="request">request</param>
    /// <param name="email">User email</param>
    /// <param name="userRole">User role</param>
    /// <returns>UserInfo</returns>
    [ProducesResponseType(typeof(UserInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [Function($"{nameof(Account)}-{nameof(GetUsers)}")]
    public Task<IEnumerable<UserInfo>> GetUsers(
        [HttpTrigger("GET", Route = "account")] HttpRequest request,
        [FromQuery, EmailAddress] string email,
        [FromQuery] UserRole userRole = UserRole.User)
    {
        logger.LogInformation("Received query param \"email\": {Email}", email);

        return Task.FromResult<IEnumerable<UserInfo>>(
        [
            new UserInfo
            {
                Id = Guid.NewGuid(),
                Email = "email@domain.com",
                Name = "User name",
            },
        ]);
    }

    [AllowAnonymous]
    [Function($"{nameof(Account)}-{nameof(CreateUser)}")]
    public Task<CreateUserResponse> CreateUser(
        [HttpTrigger("POST", Route = "account")] HttpRequest request,
        [FromBody, Required] UserInfo user)
    {
        logger.LogInformation("Received user with email: {Email}", user.Email);

        var createdUser = new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "email@domain.com",
            Name = "User name",
        };

        // attaches custom property to RequestTelemetry
        Activity.Current?.AddBaggage("UserId", createdUser.Id.ToString());

        return Task.FromResult(new CreateUserResponse
        {
            HttpResult = createdUser,
            QueueValue = createdUser,
        });
    }

    [Function($"{nameof(Account)}-{nameof(GetUserInfo)}")]
    public Task<UserInfo> GetUserInfo(
        [HttpTrigger("GET", Route = "account/user/{email}")] HttpRequest request,
        [FromRoute, EmailAddress] string email)
    {
        logger.LogInformation("Received route param \"email\": {Email}", email);

        return Task.FromResult(new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "email@domain.com",
            Name = "User name",
        });
    }

    [Function($"{nameof(Account)}-{nameof(SignIn)}")]
    public bool SignIn(
        [HttpTrigger("POST", Route = "account/sign-in")] HttpRequest request,
        [FromBody] SignInRequest signInRequest)
    {
        logger.LogInformation("Received sign in request for email: {Email}", signInRequest.Email);

        return true;
    }

    [RequestFormLimits(MultipartBodyLengthLimit = 5_000_000)]
    [Function($"{nameof(Account)}-{nameof(UploadPhoto)}")]
    public async Task UploadPhoto(
        [HttpTrigger("POST", Route = "account/upload")] HttpRequest request,
        [FromForm] UserInfo userInfo,
        [Required] IFormFile photo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received profile photo: {FileName}, \"{ContentType}\"", photo.FileName, photo.ContentType);

        using var stream = new MemoryStream();

        await photo.CopyToAsync(stream, cancellationToken);
    }

    [Function($"{nameof(Account)}-{nameof(UserCreatedQueue)}")]
    public void UserCreatedQueue(
        [QueueTrigger("user-created")] UserInfo user)
    {
        logger.LogInformation("Received user info: {FileName}", user.Email);
    }
}
