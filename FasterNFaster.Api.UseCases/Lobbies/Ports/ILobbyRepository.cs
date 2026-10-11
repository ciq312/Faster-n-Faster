using FasterNFaster.Api.Domain.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.Ports;

public interface ILobbyRepository
{
    Lobby? Get(Guid id);
    Lobby GetRequired(Guid id);
    void Add(Lobby lobby);
    void Remove(Guid id);
    void Update(Lobby lobby);
    Lobby? GetByInviteCode(string code);
    IReadOnlyCollection<Lobby> GetAll();
}
