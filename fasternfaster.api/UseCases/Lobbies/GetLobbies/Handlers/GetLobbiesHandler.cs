using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Handlers;

public class GetLobbiesHandler(ILobbyStore lobbyStore, IRaceService raceService) : IRequestHandler<GetLobbiesQuery, GetLobbiesResult>
{
    public async Task<GetLobbiesResult> Handle(GetLobbiesQuery query, CancellationToken cancellationToken)
    {
        var lobbies = query.InviteCode is null
            ? lobbyStore.GetAll()
            : FindByInviteCode(query.InviteCode);

        var items = new List<LobbyListItem>();
        foreach (var l in lobbies.OrderByDescending(l => l.LobbySettings.CreatedAt))
        {
            var raceSettings = await raceService.GetRaceSettingsOrDefault(l.Id);
            if (raceSettings is null) continue;

            items.Add(new LobbyListItem(
                l.Id,
                l.Name,
                raceSettings.RaceType,
                l.LobbySettings.IsPrivate,
                l.IsSessionActive.ToString(),
                l.Players.Count,
                l.LobbySettings.MaxPlayers,
                l.LobbySettings.CreatedAt
            ));
        }

        return new GetLobbiesResult(items);
    }

    private IReadOnlyCollection<Lobby> FindByInviteCode(string code) =>
        lobbyStore.GetByInviteCode(code) is { } lobby ? [lobby] : [];
}
