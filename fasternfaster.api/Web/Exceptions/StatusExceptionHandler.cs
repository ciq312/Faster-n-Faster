using FasterNFaster.Api.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace FasterNFaster.Api.Web.Exceptions;

public class StatusExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is StatusException ex)
        {
            httpContext.Response.StatusCode = ex.StatusCode;
            await httpContext.Response.WriteAsJsonAsync(new { error = ex.Message }, cancellationToken);
            return true;
        }

        return false;
    }
}
