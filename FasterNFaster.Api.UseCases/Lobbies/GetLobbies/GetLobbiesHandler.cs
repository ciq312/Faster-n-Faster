using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.Api.UseCases.Races.Services;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public class GetLobbiesHandler(ILobbyAccess lobbies, IRaceAccess races) : IRequestHandler<GetLobbiesQuery, GetLobbiesResult>
{
    public async Task<GetLobbiesResult> Handle(GetLobbiesQuery query, CancellationToken cancellationToken)
    {
        var lobbyIds = query.InviteCode is null
            ? lobbies.GetAllIds()
            : FindByInviteCode(query.InviteCode);

        var items = new List<LobbyListItem>();
        foreach (var lobbyId in lobbyIds)
        {
            var raceSettings = await races.GetRaceSettingsOrDefault(lobbyId);
            if (raceSettings is null) continue;

            var item = await lobbies.TryRead(lobbyId, l => new LobbyListItem(
                l.Id,
                l.Name,
                l.LobbySettings.IsPrivate,
                l.IsSessionActive,
                l.Players.Count,
                l.LobbySettings.MaxPlayers,
                l.LobbySettings.CreatedAt
            ));
            if (item is null) continue;

            items.Add(item);
        }

        return new GetLobbiesResult([.. items.OrderByDescending(i => i.CreatedAt)]);
    }

    private IReadOnlyCollection<Guid> FindByInviteCode(string code) =>
        lobbies.FindIdByInviteCode(code) is { } id ? [id] : [];
}
