namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyStateTracker
{
    bool TryBeginScope();
    void MarkChanged(Guid lobbyId);
    IReadOnlyCollection<Guid> EndScope();
}
