using Microsoft.AspNetCore.Http;

namespace Microsoft.Azure.Functions.Worker;

public static class HttpExtensions
{
    public static FunctionContext? GetFunctionContext(this HttpContext httpContext)
    {
        return httpContext.Features.Get<FunctionContext>();
    }
}
