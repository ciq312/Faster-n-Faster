using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Tests.Fakes;

public class FakeLobbyStateBroadcaster : ILobbyStateBroadcaster
{
    public List<Lobby> ByLobby { get; } = new();
    public List<Guid> ByLobbyId { get; } = new();

    public Task BroadcastLobbyState(Lobby lobby)
    {
        ByLobby.Add(lobby);
        return Task.CompletedTask;
    }

    public Task BroadcastLobbyState(Guid lobbyId)
    {
        ByLobbyId.Add(lobbyId);
        return Task.CompletedTask;
    }
}
