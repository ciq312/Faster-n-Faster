using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Handlers;

public class GetLobbiesHandler(ILobbyStore lobbyStore, IRaceService raceService) : IRequestHandler<GetLobbiesQuery, GetLobbiesResult>
{
    public Task<GetLobbiesResult> Handle(GetLobbiesQuery query, CancellationToken cancellationToken)
    {
        var lobbies = lobbyStore
            .GetAll()
            .OrderByDescending(l => l.LobbySettings.CreatedAt)
            .Select(l => new LobbyListItem(
                l.Id,
                l.Name,
                raceService.GetRaceSettings(l.Id).RaceType,
                l.LobbySettings.IsPrivate,
                l.IsSessionActive.ToString(),
                l.Players.Count,
                l.LobbySettings.MaxPlayers,
                l.LobbySettings.CreatedAt
            ))
            .ToList();

        return Task.FromResult(new GetLobbiesResult(lobbies));
    }
}
