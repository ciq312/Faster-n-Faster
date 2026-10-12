using FasterNFaster.Api.Domain.Entities.Lobbies.Colors;

namespace FasterNFaster.Api.UseCases.Lobbies.LobbyState;

public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    bool IsSessionActive,
    RaceSettingsDTO Settings,
    bool IsPrivate,
    string? InviteCode,
    int MaxPlayers,
    IReadOnlyList<ColorStatus> Colors,
    IReadOnlyList<LobbyPlayerDTO> Players);
