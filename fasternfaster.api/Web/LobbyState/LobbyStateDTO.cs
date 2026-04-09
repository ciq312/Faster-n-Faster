using FasterNFaster.Api.Core.Lobbies.Colors;

namespace FasterNFaster.Api.Web.Lobbies.LobbyState;

public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    string RaceType,
    IRaceSettings Settings,
    bool IsPrivate,
    string? InviteCode,
    int MaxPlayers,
    IEnumerable<ColorStatus> Colors,
    IReadOnlyList<LobbyPlayerDto> Players);

public record LobbyPlayerDto(Guid Id, bool IsHost, string Nick, int JoinOrder, bool IsConnected, string Color);
