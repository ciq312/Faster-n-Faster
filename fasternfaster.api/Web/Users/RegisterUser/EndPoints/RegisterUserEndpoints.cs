using FastEndpoints;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.Commands;
using FasterNFaster.Api.UseCases.Users.RegisterUsers.DTO;
using MediatR;

namespace FasterNFaster.Api.Web.Users.RegisterUser;

public class RegisterUserEndpoint(ISender sender) : Endpoint<RegisterUserRequest, RegisterUserResult>
{
    public override void Configure()
    {
        Post("/api/auth/register");
        AllowAnonymous();
    }

    public override async Task HandleAsync(RegisterUserRequest req, CancellationToken ct)
    {
        var result = await sender.Send(new RegisterUserCommand(req.Nick, req.Login, req.Email, req.Password), ct);
        await Send.CreatedAtAsync<RegisterUserEndpoint>(new { UserID = result.UserId }, result, cancellation: ct);
    }
}
