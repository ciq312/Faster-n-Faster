
using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Core.Lobbies.Colors;

public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    Race? GameMode,
    bool IsPrivate,
    int MaxPlayers,
    IEnumerable<ColorStatus> Colors,
    IReadOnlyList<LobbyPlayerDto> Players);
public record LobbyPlayerDto(Guid Id, bool IsHost, string Nick, int JoinOrder, bool IsConnected, string Color);
