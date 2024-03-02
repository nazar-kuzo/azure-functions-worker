using AzureFunctions.Worker.Extensions.TestHost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace AzureFunctions.Worker.Extensions.TestHost.Functions;

[Authorize]
public class Account(ILogger<Account> logger)
{
    [Function($"{nameof(Account)}-{nameof(GetUsers)}")]
    public Task<IEnumerable<UserInfo>> GetUsers(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "account")] HttpRequest request,
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

    [Function($"{nameof(Account)}-{nameof(CreateUser)}")]
    public Task<UserInfo> CreateUser(
        [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "account")] HttpRequest request,
        [FromBody, Required] UserInfo user)
    {
        logger.LogInformation("Received user with email: {Email}", user.Email);

        return Task.FromResult(new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "email@domain.com",
            Name = "User name",
        });
    }

    [Function($"{nameof(Account)}-{nameof(GetUserInfo)}")]
    public Task<UserInfo> GetUserInfo(
        [HttpTrigger(AuthorizationLevel.Anonymous, "GET", Route = "account/user/{email}")] HttpRequest request,
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
        [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "account/sign-in")] HttpRequest request,
        [FromBody] SignInRequest signInRequest)
    {
        logger.LogInformation("Received sign in request for email: {Email}", signInRequest.Email);

        return true;
    }

    [RequestFormLimits(MultipartBodyLengthLimit = 5_000_000)]
    [Function($"{nameof(Account)}-{nameof(UploadPhoto)}")]
    public async Task UploadPhoto(
        [HttpTrigger(AuthorizationLevel.Anonymous, "POST", Route = "account/upload")] HttpRequest request,
        [Required] IFormFile photo,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("Received profile photo: {FileName}, \"{ContentType}\"", photo.FileName, photo.ContentType);

        using var stream = new MemoryStream();

        await photo.CopyToAsync(stream, cancellationToken);
    }
}
