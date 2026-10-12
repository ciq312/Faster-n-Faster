namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public record LobbyListItem(
    Guid Id,
    string Name,
    bool IsPrivate,
    bool IsSessionActive,
    int PlayerCount,
    int MaxPlayers,
    DateTime CreatedAt
);
