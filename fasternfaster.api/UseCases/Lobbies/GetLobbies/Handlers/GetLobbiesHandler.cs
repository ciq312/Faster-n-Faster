using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Helpers;
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
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new LobbyListItem(
                l.Id,
                l.Name,
                l.WordRace != null ? "wordcount"
                    : l.TimerRace != null ? "timer"
                    : null,
                l.IsPrivate,
                l.Status,
                l.Players.Count(p => p.IsConnected),
                l.MaxPlayers,
                l.CreatedAt
            ))
            .ToList();

        Log.Information("Got lobbies");
        return Task.FromResult(new GetLobbiesResult(lobbies));
    }
}
