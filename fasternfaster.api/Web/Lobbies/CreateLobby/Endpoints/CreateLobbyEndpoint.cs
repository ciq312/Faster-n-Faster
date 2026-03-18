using FastEndpoints;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobby.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobby.CreateLobby.Results;

namespace FasterNFaster.Api.Web.Lobbies.CreateLobby.Endpoints;

public class CreateLobbyRequest
{
    public string? LobbyName { get; set; }
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
        AllowAnonymous();
    }

    public override async Task HandleAsync(CreateLobbyRequest req, CancellationToken ct)
    {
        var command = new CreateLobbyCommand(
            req.LobbyName,
            req.DisplayName,
            req.GameMode,
            req.IsPrivate,
            req.WordCount,
            req.TimerDurationSeconds
        );

        var result = await _handler.Handle(command);
        await Send.CreatedAtAsync("CreateLobby", null, result);
    }
}
