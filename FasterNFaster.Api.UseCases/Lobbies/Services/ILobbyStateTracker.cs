namespace FasterNFaster.Api.UseCases.Lobbies.Services;

public interface ILobbyStateTracker
{
    bool TryBeginScope();
    void MarkChanged(Guid lobbyId);
    IReadOnlyCollection<Guid> EndScope();
}
