using FasterNFaster.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace FasterNFaster.Api.Presentation.Web.Shared;

public class StatusExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not StatusException ex) return false;

        httpContext.Response.StatusCode = MapStatusCode(ex);
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

    private static int MapStatusCode(StatusException ex) =>
        ex switch
        {
            ForbiddenException => StatusCodes.Status403Forbidden,
            BadRequestException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            _ => StatusCodes.Status500InternalServerError,
        };
}
