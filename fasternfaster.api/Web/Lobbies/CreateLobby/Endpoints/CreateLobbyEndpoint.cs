using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;

namespace FasterNFaster.Api.Web.Lobbies.CreateLobby.Endpoints;

public class CreateLobbyRequest
{
    public string LobbyName { get; set; } = null!;
    public string DisplayName { get; set; } = null!;
    public string GameMode { get; set; } = null!;
    public bool IsPrivate { get; set; }
    public int? WordCount { get; set; }
    public int? TimerDurationSeconds { get; set; }
}

public class CreateLobbyEndpoint : Endpoint<CreateLobbyRequest, CreateLobbyResult>
{
    private readonly IHandler<CreateLobbyCommand, CreateLobbyResult> _handler;

    public CreateLobbyEndpoint(IHandler<CreateLobbyCommand, CreateLobbyResult> handler)
    {
        _handler = handler;
    }

    public override void Configure()
    {
        Post("/api/lobbies");
    }

    public override async Task HandleAsync(CreateLobbyRequest req, CancellationToken ct)
    {
        var userId = Guid.Parse(
            HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        var command = new CreateLobbyCommand(
            req.LobbyName,
            req.DisplayName,
            req.GameMode,
            req.IsPrivate,
            req.WordCount,
            req.TimerDurationSeconds,
            userId
        );

        var result = await _handler.Handle(command);
        await Send.CreatedAtAsync("CreateLobby", null, result);
    }
}
