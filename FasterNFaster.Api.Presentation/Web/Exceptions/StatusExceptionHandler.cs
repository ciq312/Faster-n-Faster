using FasterNFaster.Api.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace FasterNFaster.Api.Web.Exceptions;

public class StatusExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not StatusException ex) return false;

        httpContext.Response.StatusCode = ex.StatusCode;
        await httpContext.Response.WriteAsJsonAsync(ToResponseBody(ex), cancellationToken);
        return true;
    }

    private static Dictionary<string, object?> ToResponseBody(StatusException ex)
    {
        var body = new Dictionary<string, object?> { ["error"] = ex.Message };

        if (ex.Code is not null)
        {
            body["code"] = ex.Code;
        }

        if (ex.Extensions is not null)
        {
            foreach (var (key, value) in ex.Extensions)
            {
                body.TryAdd(key, value);
            }
        }

        return body;
    }
}
