
namespace FasterNFaster.Api.UseCases.Lobbies.CreateLobby.Commands;

public record CreateLobbyCommand(
    string LobbyName,
    bool IsPrivate,
    Guid HostId
);
