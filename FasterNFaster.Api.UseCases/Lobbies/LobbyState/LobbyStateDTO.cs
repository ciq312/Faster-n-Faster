using FasterNFaster.Api.Core.Entities.Lobbies.Colors;
using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.UseCases.LobbyState;

public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    string RaceType,
    bool IsSessionActive,
    IRaceSettings Settings,
    bool IsPrivate,
    string? InviteCode,
    int MaxPlayers,
    IEnumerable<ColorStatus> Colors,
    IReadOnlyList<LobbyPlayerDTO> Players);

