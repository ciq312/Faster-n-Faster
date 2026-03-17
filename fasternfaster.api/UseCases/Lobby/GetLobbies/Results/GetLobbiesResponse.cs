namespace FasterNFaster.Api.UseCases.Lobby.GetLobbies.Results;

public record GetLobbiesResponse(IReadOnlyList<LobbyListItem> Lobbies);

public record LobbyListItem(
    Guid Id,
    string Name,
    string? GameMode,
    int PlayerCount,
    int MaxPlayers,
    DateTime CreatedAt);
