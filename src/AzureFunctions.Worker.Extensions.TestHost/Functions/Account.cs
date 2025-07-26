using System.Diagnostics;
using AzureFunctions.Worker.Extensions.TestHost.ExceptionHandling;
using AzureFunctions.Worker.Extensions.TestHost.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Caching.Distributed;

namespace AzureFunctions.Worker.Extensions.TestHost.Functions;

/// <summary>
/// Account API
/// </summary>
/// <param name="logger">Logger</param>
/// <param name="cache">Distributed cache</param>
[Authorize]
public class Account(ILogger<Account> logger, IDistributedCache cache)
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
    public async Task<CreateUserResponse> CreateUser(
        [HttpTrigger("POST", Route = "account")] HttpRequest request,
        [FromBody, Required] UserInfo user)
    {
        // showcases global exception handling
        if (user.Email == "invalid@email")
        {
            throw new BadRequestException("Invalid email");
        }

        logger.LogInformation("Received user with email: {Email}", user.Email);

        var createdUser = new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "email@domain.com",
            Name = "User name",
        };

        // attaches custom property to RequestTelemetry
        Activity.Current?.AddBaggage("UserId", createdUser.Id.ToString());

        await cache.SetJsonAsync(user.Email, createdUser, new() { AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1) });

        return new CreateUserResponse
        {
            HttpResult = createdUser,
            QueueValue = new QueueMessage<UserInfo>
            {
                TraceParent = Activity.Current?.Id,
                TraceState = Activity.Current?.TraceStateString,
                Baggage = Activity.Current?.Baggage,
                Data = createdUser,
            },
        };
    }

    [Function($"{nameof(Account)}-{nameof(GetUserInfo)}")]
    public async Task<UserInfo> GetUserInfo(
        [HttpTrigger("GET", Route = "account/user/{email}")] HttpRequest request,
        [FromRoute, EmailAddress] string email)
    {
        logger.LogInformation("Received route param \"email\": {Email}", email);

        var userInfo = await cache.GetJsonAsync<UserInfo>(email);

        return userInfo ?? new UserInfo
        {
            Id = Guid.NewGuid(),
            Email = "email@domain.com",
            Name = "User name",
        };
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
        [QueueTrigger("user-created")] QueueMessage<UserInfo> message)
    {
        Activity.Current?.ApplyCorrelationContext(message.TraceParent, message.TraceState, message.Baggage);

        logger.LogInformation("Received user info: {FileName}", message.Data.Email);
    }
}
