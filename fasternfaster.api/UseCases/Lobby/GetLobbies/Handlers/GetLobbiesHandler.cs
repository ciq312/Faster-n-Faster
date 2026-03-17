using FasterNFaster.Api.Infrastructure.Data;
using FasterNFaster.Api.UseCases.Helpers;
using FasterNFaster.Api.UseCases.Lobby.GetLobbies.Queries;
using FasterNFaster.Api.UseCases.Lobby.GetLobbies.Results;
using Microsoft.EntityFrameworkCore;

namespace FasterNFaster.Api.UseCases.Lobby.GetLobbies.Handlers;

public class GetLobbiesHandler : IHandler<GetLobbiesQuery, GetLobbiesResponse>
{
    private readonly AppDbContext _db;

    public GetLobbiesHandler(AppDbContext db)
    {
        _db = db;
    }

    public async Task<GetLobbiesResponse> Handle(GetLobbiesQuery query)
    {
        var lobbies = await _db.Lobbies
            .Where(l => l.Status == "waiting" && !l.IsPrivate)
            .OrderByDescending(l => l.CreatedAt)
            .Select(l => new LobbyListItem(
                l.Id,
                l.Name,
                l.WordRace != null ? l.WordRace.GameMode : l.TimerRace != null ? l.TimerRace.GameMode : null,
                l.Players.Count(p => p.IsConnected),
                l.MaxPlayers,
                l.CreatedAt))
            .ToListAsync();

        return new GetLobbiesResponse(lobbies);
    }
}
