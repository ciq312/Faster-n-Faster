
using FasterNFaster.Api.Core.Entities.Lobbies.Races;

public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    Race? GameMode,
    bool IsPrivate,
    IReadOnlyList<LobbyPlayerDto> Players);
public record LobbyPlayerDto(Guid Id, bool IsHost, string Nick, int JoinOrder, bool IsConnected);
