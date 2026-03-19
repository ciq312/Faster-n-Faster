namespace FasterNFaster.Api.UseCases.Lobbies.GetLobbies.Results;

public record GetLobbiesResult(IReadOnlyList<LobbyListItem> Lobbies);

public record LobbyListItem(
    Guid Id,
    string Name,
    string? GameMode,
    bool IsPrivate,
    string Status,
    int PlayerCount,
    int MaxPlayers,
    DateTime CreatedAt
);
