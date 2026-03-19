using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;

namespace FasterNFaster.Api.Web.Lobbies.JoinLobby.Endpoints;

public class JoinLobbyRequest
{
    public Guid LobbyId { get; set; }
}

public class JoinLobbyEndpoint : Endpoint<JoinLobbyRequest, JoinLobbyResult>
{
    private readonly IHandler<JoinLobbyCommand, JoinLobbyResult> _handler;

    public JoinLobbyEndpoint(IHandler<JoinLobbyCommand, JoinLobbyResult> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/lobbies/{LobbyId}/join");
    }

    public override async Task HandleAsync(JoinLobbyRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var command = new JoinLobbyCommand(userId, req.LobbyId);
        var result = await _handler.Handle(command);
        await Send.OkAsync(result, ct);
    }
}
