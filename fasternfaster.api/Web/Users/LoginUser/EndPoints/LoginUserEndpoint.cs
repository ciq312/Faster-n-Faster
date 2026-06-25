using FastEndpoints;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Users.LoginUsers;
using FasterNFaster.Api.Web.Services.Interfaces;
using FasterNFaster.Api.Web.Users.LoginUser.Endpoints;
using MediatR;

namespace FasterNFaster.Api.Web.Users.LoginUser;

public class LoginUserEndpoint(ISender sender, ITokenService tokenService, ISessionService sessions) : Endpoint<LoginUserRequest, LoginUserResult>
{
    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginUserRequest req, CancellationToken ct)
    {
        try
        {
            var result = await sender.Send(new LoginUserCommand(req.Login, req.Password), ct);
            await tokenService.HandlePlayerAuth(result.UserId, result.Nick);
            await Send.OkAsync(result, cancellation: ct);
        }
        catch (InvalidCredentialsException e)
        {
            ThrowError(e.Message, 401);
        }
        catch (EmailNotVerifiedException e)
        {
            HttpContext.Response.StatusCode = 403;
            await HttpContext.Response.WriteAsJsonAsync(new { code = "EMAIL_NOT_VERIFIED", email = e.Email }, cancellationToken: ct);
        }
    }
}
