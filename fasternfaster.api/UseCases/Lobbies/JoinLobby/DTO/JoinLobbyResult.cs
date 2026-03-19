namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;

public record JoinLobbyResult(
    Guid LobbyId,
    string LobbyName,
    string? GameMode,
    bool IsPrivate,
    Guid PlayerId,
    Guid HostPlayerId,
    IReadOnlyList<LobbyPlayerDto> Players
);

public record LobbyPlayerDto(Guid Id, string DisplayName, int JoinOrder, bool IsConnected);
