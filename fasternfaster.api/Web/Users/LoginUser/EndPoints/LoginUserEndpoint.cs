using FastEndpoints;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Auth;
using FasterNFaster.Api.UseCases.Users.LoginUsers;
using FasterNFaster.Api.Web.Services;
using FasterNFaster.Api.Web.Services.Interfaces;
using FasterNFaster.Api.Web.Users.LoginUser.Endpoints;

namespace FasterNFaster.Api.Web.Users.LoginUser;

public class LoginUserEndpoint(
    IHandler<LoginUserCommand, LoginUserResult> handler,
    ITokenService tokenService,
    ISessionService sessions) : Endpoint<LoginUserRequest, LoginUserResult>
{

    private readonly IHandler<LoginUserCommand, LoginUserResult> handler = handler;
    private readonly ITokenService tokenService = tokenService;
    private readonly ISessionService sessions = sessions;

    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginUserRequest req, CancellationToken ct)
    {
        try
        {
            var result = await handler.Handle(new LoginUserCommand(req.Login, req.Password));

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