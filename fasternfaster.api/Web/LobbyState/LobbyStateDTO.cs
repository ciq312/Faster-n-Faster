using FasterNFaster.Api.Core.Lobbies.Colors;

public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    RaceSettingsDto RaceSettings,
    bool IsPrivate,
    int MaxPlayers,
    IEnumerable<ColorStatus> Colors,
    IReadOnlyList<LobbyPlayerDto> Players);

public record RaceSettingsDto(
    string GameMode,
    int WordCount,
    int TimerDuration,
    string? Passage);

public record LobbyPlayerDto(Guid Id, bool IsHost, string Nick, int JoinOrder, bool IsConnected, string Color);
