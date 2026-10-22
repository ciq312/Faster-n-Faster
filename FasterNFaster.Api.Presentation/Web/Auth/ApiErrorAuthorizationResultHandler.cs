using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.Presentation.Web.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;

namespace FasterNFaster.Api.Presentation.Web.Auth;

public class ApiErrorAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler defaultHandler = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context, AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        await defaultHandler.HandleAsync(next, context, policy, result);

        if (result.Challenged)
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse { Reason = "Authentication required", Code = ErrorCodes.NotAuthenticated });
        else if (result.Forbidden)
            await context.Response.WriteAsJsonAsync(new ApiErrorResponse { Reason = "Access denied", Code = ErrorCodes.Forbidden });
    }
}
