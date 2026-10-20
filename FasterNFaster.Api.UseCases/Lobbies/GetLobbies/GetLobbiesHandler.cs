using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public class GetLobbiesHandler(ILobbyStore lobbies, IRaceStore races) : IRequestHandler<GetLobbiesQuery, GetLobbiesResult>
{
    public async Task<GetLobbiesResult> Handle(GetLobbiesQuery query, CancellationToken cancellationToken)
    {
        var lobbyIds = query.InviteCode is null
            ? lobbies.GetAllIds()
            : FindByInviteCode(query.InviteCode);

        var items = new List<LobbyListItem>();
        foreach (var lobbyId in lobbyIds)
        {
            if (!races.Exists(lobbyId)) continue;

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
