using System.Security.Claims;
using FastEndpoints;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Results;

namespace FasterNFaster.Api.Web.Lobbies.CreateLobby.Endpoints;

public class CreateLobbyRequest
{
    public string LobbyName { get; set; } = null!;
    public bool IsPrivate { get; set; }
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
        Roles("Player", "Guest");
    }

    public override async Task HandleAsync(CreateLobbyRequest req, CancellationToken ct)
    {
        Console.WriteLine($"IsAuthenticated: {User.Identity?.IsAuthenticated}");
        Console.WriteLine($"Claims: {string.Join(" | ", User.Claims.Select(c =>
    $"{c.Type}={c.Value}"))}");

        var userIdClaim = User.FindFirstValue("sub");
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var command = new CreateLobbyCommand(
            req.LobbyName,
            req.IsPrivate,
            userId
        );
        try
        {
            var result = await _handler.Handle(command);
            await Send.CreatedAtAsync("CreateLobby", null, result);
        }
        catch (InvalidOperationException e) { ThrowError(e.Message, 400); }
    }
}
