using Tapper;

namespace FasterNFaster.Api.UseCases.Lobbies.LobbyState;

[TranspilationSource]
public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    bool IsRaceStarting,
    bool IsRaceStarted,
    RaceSettingsDTO Settings,
    bool IsPrivate,
    string? InviteCode,
    int MaxPlayers,
    IReadOnlyList<ColorStatusDTO> Colors,
    IReadOnlyList<LobbyPlayerDTO> Players);
