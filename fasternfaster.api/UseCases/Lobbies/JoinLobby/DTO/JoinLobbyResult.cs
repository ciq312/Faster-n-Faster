
using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Lobbies.JoinLobby.Results;

public record JoinLobbyResult(
    Guid LobbyId,
    string LobbyName,
    Race? GameMode,
    bool IsPrivate,
    IReadOnlyList<LobbyPlayerDto> Players
);

public record LobbyPlayerDto(Guid Id, bool IsHost, string Nick, int JoinOrder, bool IsConnected);
