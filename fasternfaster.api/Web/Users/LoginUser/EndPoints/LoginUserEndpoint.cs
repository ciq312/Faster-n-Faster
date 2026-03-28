using FastEndpoints;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.LoginUsers;
using FasterNFaster.Api.Web.Users.LoginUser.Endpoints;

namespace FasterNFaster.Api.Web.Users.LoginUser;

public class LoginUserEndpoint(IHandler<LoginUserCommand, LoginUserResult> handler) : Endpoint<LoginUserRequest, LoginUserResult>
{

    public IHandler<LoginUserCommand, LoginUserResult> _handler = handler;

    public override void Configure()
    {
        Post("/api/auth/login");
        AllowAnonymous();
    }

    public override async Task HandleAsync(LoginUserRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _handler.Handle(new LoginUserCommand(req.Login, req.Password));
            await Send.OkAsync(result, cancellation: ct);

        }
        catch (InvalidCredentialsException e)
        {
            ThrowError(e.Message, 401);
        }



    }
}