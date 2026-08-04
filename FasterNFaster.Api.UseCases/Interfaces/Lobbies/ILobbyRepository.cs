using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Interfaces.Lobbies;

public interface ILobbyRepository
{
    Lobby? Get(Guid id);
    Lobby GetRequired(Guid id);
    void Add(Lobby lobby);
    void Remove(Guid id);
    void Update(Lobby lobby);
    Lobby? GetByInviteCode(string code);
    IReadOnlyCollection<Lobby> GetAll();
    Task SaveChanges();
}
