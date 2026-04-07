using FastEndpoints;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.Web.Lobbies.FindLobbyByCode.Endpoints;

public class FindLobbyByCodeRequest
{
    public string Code { get; set; } = null!;
}

public class FindLobbyByCodeResponse
{
    public Guid LobbyId { get; set; }
}

public class FindLobbyByCodeEndpoint : Endpoint<FindLobbyByCodeRequest, FindLobbyByCodeResponse>
{
    private readonly ILobbyStore _lobbyStore;

    public FindLobbyByCodeEndpoint(ILobbyStore lobbyStore)
    {
        _lobbyStore = lobbyStore;
    }

    public override void Configure()
    {
        Get("/api/lobbies/by-code/{Code}");
    }

    public override async Task HandleAsync(FindLobbyByCodeRequest req, CancellationToken ct)
    {
        var lobby = _lobbyStore.GetByInviteCode(req.Code);

        if (lobby == null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(new FindLobbyByCodeResponse { LobbyId = lobby.Id }, ct);
    }
}
