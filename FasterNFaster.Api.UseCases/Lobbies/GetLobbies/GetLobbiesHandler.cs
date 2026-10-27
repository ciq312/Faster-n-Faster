using FasterNFaster.Api.UseCases.Lobbies.Ports;
using FasterNFaster.Api.UseCases.Races.Ports;
using MediatR;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public class GetLobbiesHandler(ILobbyStore lobbies, IRaceStore races) : IRequestHandler<GetLobbiesQuery, GetLobbiesResult>
{
    private const int MaxLimit = 100;

    public async Task<GetLobbiesResult> Handle(GetLobbiesQuery query, CancellationToken cancellationToken)
    {
        var limit = Math.Clamp(query.Limit, 1, MaxLimit);
        var after = query.Cursor is null ? null : LobbyCursor.Decode(query.Cursor);

        var publicLobbies = await ReadPublicLobbies();
        var page = publicLobbies
            .Where(i => after is null || after.Precedes(i))
            .OrderByDescending(i => i.CreatedAt)
            .ThenBy(i => i.Id)
            .Take(limit + 1)
            .ToList();

        var nextCursor = page.Count > limit ? LobbyCursor.After(page[limit - 1]).Encode() : null;
        return new GetLobbiesResult([.. page.Take(limit)], nextCursor);
    }

    private async Task<List<LobbyListItem>> ReadPublicLobbies()
    {
        var items = new List<LobbyListItem>();
        foreach (var lobbyId in lobbies.GetAllIds())
        {
            if (!races.Exists(lobbyId)) continue;

            var item = await lobbies.TryRead(lobbyId, l => l.LobbySettings.IsPrivate ? null : LobbyListItem.From(l));
            if (item is not null) items.Add(item);
        }

        return items;
    }
}
