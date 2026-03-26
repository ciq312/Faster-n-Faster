using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.Core.Interfaces;

public interface ILobbyStore
{
    void Add(Lobby lobby);
    Lobby? Get(Guid id);
    IReadOnlyCollection<Lobby> GetAll();
}
