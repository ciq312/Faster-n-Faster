using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Handlers;

public class GetLobbiesHandler(ILobbyStore lobbyStore) : IHandler<GetLobbiesQuery, GetLobbiesResult>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;

    public Task<GetLobbiesResult> Handle(GetLobbiesQuery query)
    {
        var lobbies = lobbyStore
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

#if DEBUG
        Log.Information("Got lobbies");
#endif
        return Task.FromResult(new GetLobbiesResult(lobbies));
    }
}
