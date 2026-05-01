using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyStore
{
    void Add(Lobby lobby);
    void Remove(Guid id);
    Task Mutate(Guid lobbyId, Action<Lobby> mutate);
    Task<Guid> GetLobbyPlayerIn(Guid userId);
    Lobby? Get(Guid id);
    Lobby GetRequired(Guid id);
    Lobby? GetByInviteCode(string code);
    IReadOnlyCollection<Lobby> GetAll();
}
