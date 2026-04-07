using FasterNFaster.Api.Core.Lobbies.Colors;

public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    RaceSettingsDto RaceSettings,
    bool IsPrivate,
    string? InviteCode,
    int MaxPlayers,
    IEnumerable<ColorStatus> Colors,
    IReadOnlyList<LobbyPlayerDto> Players);

public record RaceSettingsDto(
    Type GameMode,
    IRaceSettings RaceSettings);

public record LobbyPlayerDto(Guid Id, bool IsHost, string Nick, int JoinOrder, bool IsConnected, string Color);
