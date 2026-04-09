using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Handlers;

public class GetLobbiesHandler : IHandler<GetLobbiesQuery, GetLobbiesResult>
{
    private readonly ILobbyStore _lobbyStore;

    public GetLobbiesHandler(ILobbyStore lobbyStore)
    {
        _lobbyStore = lobbyStore;
    }

    public Task<GetLobbiesResult> Handle(GetLobbiesQuery query)
    {
        var lobbies = _lobbyStore
            .GetAll()
            .OrderByDescending(l => l.LobbySettings.CreatedAt)
            .Select(l => new LobbyListItem(
                l.Id,
                l.Name,
                l.Race.GetType().ToString(),
                l.LobbySettings.IsPrivate,
                l.IsSessionActive.ToString(),
                l.Players.Count,
                l.LobbySettings.MaxPlayers,
                l.LobbySettings.CreatedAt
            ))
            .ToList();

        Log.Information("Got lobbies");
        return Task.FromResult(new GetLobbiesResult(lobbies));
    }
}
