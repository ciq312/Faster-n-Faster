using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;
using MediatR;

namespace FasterNFaster.Api.Web.Lobbies.CreateLobby.Endpoints;

public class CreateLobbyRequest
{
    public string LobbyName { get; set; } = null!;
    public bool IsPrivate { get; set; }
}

public class CreateLobbyEndpoint(ISender sender) : Endpoint<CreateLobbyRequest, CreateLobbyResult>
{
    public override void Configure()
    {
        Post("/api/lobbies");
        Roles("Player", "Guest");
    }

    public override async Task HandleAsync(CreateLobbyRequest req, CancellationToken ct)
    {
        var userIdClaim = User.FindFirstValue("sub");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await sender.Send(new CreateLobbyCommand(req.LobbyName, req.IsPrivate, userId), ct);
        await Send.CreatedAtAsync("CreateLobby", null, result);
    }
}
