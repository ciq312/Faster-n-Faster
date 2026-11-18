using FasterNFaster.Api.Domain.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public record LobbyListItem(
    Guid Id,
    string Name,
    bool IsPrivate,
    bool IsSessionActive,
    int PlayerCount,
    int MaxPlayers,
    DateTimeOffset CreatedAt
)
{
    public static LobbyListItem From(Lobby lobby) =>
        new(
            lobby.Id,
            lobby.Name,
            lobby.LobbySettings.IsPrivate,
            lobby.IsSessionActive,
            lobby.Players.Count,
            lobby.LobbySettings.MaxPlayers,
            lobby.LobbySettings.CreatedAt
        );
}
