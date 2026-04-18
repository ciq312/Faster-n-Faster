using FastEndpoints;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.LoginUsers;
using FasterNFaster.Api.Web.Services.Interfaces;
using FasterNFaster.Api.Web.Users.LoginUser.Endpoints;

namespace FasterNFaster.Api.Web.Users.LoginUser;

public class LoginUserEndpoint(IHandler<LoginUserCommand, LoginUserResult> handler, ITokenService tokenService) : Endpoint<LoginUserRequest, LoginUserResult>
{

    private readonly IHandler<LoginUserCommand, LoginUserResult> handler = handler;
    private readonly ITokenService tokenService = tokenService;

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
    }
}