using System.Data;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.DTO;
using FasterNFaster.Api.Web.Services;

namespace FasterNFaster.Api.Web.Users.RegisterUser;

public class RegisterUserEndpoint(IHandler<RegisterUserCommand, RegisterUserResult> handler) : Endpoint<RegisterUserRequest, TokenCreationRequest>
{
    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterUserRequest req, CancellationToken ct)
    {
        try
        {
            var result = await handler.Handle(new RegisterUserCommand(req.Nick, req.Login, req.Password));

            await Send.OkAsync(ct);

        }
        catch (DuplicateNickException e)
        {
            ThrowError(e.Message, 409);
        }
        catch (DuplicateLoginException e)
        {
            ThrowError(e.Message, 409);
        }
    }
}
