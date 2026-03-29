using System.Data;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.DTO;

public class RegisterUserEndpoint : Endpoint<RegisterUserRequest, RegisterUserResult>
{

    public IHandler<RegisterUserCommand, RegisterUserResult> _handler;

    public RegisterUserEndpoint(IHandler<RegisterUserCommand, RegisterUserResult> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
    }
    public override async Task HandleAsync(RegisterUserRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _handler.Handle(new RegisterUserCommand(req.Nick, req.Login, req.Password));

            await Send.OkAsync(result, cancellation: ct);
        }
        catch (DuplicateNameException e)
        {
            ThrowError(e.Message, 409);
        }
        catch (DuplicateLoginException e)
        {
            ThrowError(e.Message, 409);
        }
    }
}



