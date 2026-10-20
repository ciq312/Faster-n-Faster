using FasterNFaster.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;

namespace FasterNFaster.Api.Presentation.Web.Shared;

public class StatusExceptionHandler : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not StatusException ex) return false;

        httpContext.Response.StatusCode = MapStatusCode(ex);
        await httpContext.Response.WriteAsJsonAsync(new ApiErrorResponse(ex.Message, ex.Code ?? "error", ex.Extensions?.ToDictionary(kvp => kvp.Key, kvp => kvp.Value)), cancellationToken);
        return true;
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
