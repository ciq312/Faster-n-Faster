using FasterNFaster.Api.Domain.Entities.Lobbies.Colors;
using FasterNFaster.Api.Domain.Entities.Races;

namespace FasterNFaster.Api.UseCases.Lobbies.LobbyState;

public record LobbyStateDTO(
    Guid LobbyId,
    string LobbyName,
    string RaceType,
    bool IsSessionActive,
    IRaceSettings Settings,
    bool IsPrivate,
    string? InviteCode,
    int MaxPlayers,
    IReadOnlyList<ColorStatus> Colors,
    IReadOnlyList<LobbyPlayerDTO> Players);

