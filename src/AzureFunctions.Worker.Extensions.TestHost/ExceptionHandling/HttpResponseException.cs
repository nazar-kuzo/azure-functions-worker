using Microsoft.AspNetCore.Http;

namespace AzureFunctions.Worker.Extensions.TestHost.ExceptionHandling;

/// <summary>
/// Client application exception with ability to specify <see cref="StatusCode"/> in response.
/// </summary>
public abstract class HttpResponseException(string message) : Exception(message)
{
    public virtual int StatusCode { get; }
}

public class BadRequestException(string message) : HttpResponseException(message)
{
    public override int StatusCode => StatusCodes.Status400BadRequest;
}

public class NotFoundException(string message) : HttpResponseException(message)
{
    public override int StatusCode => StatusCodes.Status404NotFound;
}

public class CustomHttpResponseException(int statusCode, string message) : HttpResponseException(message)
{
    public override int StatusCode => statusCode;
}

public class ServerException(string message) : HttpResponseException(message)
{
    public override int StatusCode => StatusCodes.Status500InternalServerError;
}

public class RestApiException(string message) : ServerException(message)
{
    public string? ErrorType { get; set; }

    public string? RequestUri { get; set; }

    public int? ResponseCode { get; set; }

    public string? ResponseMessage { get; set; }

    public static async Task ThrowAsync(string message, HttpResponseMessage response, string? errorType = null)
    {
        throw new RestApiException(message)
        {
            ErrorType = errorType,
            ResponseCode = (int) response.StatusCode,
            ResponseMessage = await response.Content.ReadAsStringAsync(),
            RequestUri = $"[{response.RequestMessage?.Method}] {response.RequestMessage?.RequestUri?.AbsoluteUri}",
        };
    }
}