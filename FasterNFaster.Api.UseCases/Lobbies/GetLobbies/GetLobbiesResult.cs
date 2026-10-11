namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies;

public record GetLobbiesResult(IReadOnlyList<LobbyListItem> Lobbies);

public record LobbyListItem(
    Guid Id,
    string Name,
    string? GameMode,
    bool IsPrivate,
    bool IsSessionActive,
    int PlayerCount,
    int MaxPlayers,
    DateTime CreatedAt
);
