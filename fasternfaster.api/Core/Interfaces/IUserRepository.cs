using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Core.Interfaces;

public interface IUserRepository
{
    void Add(User user);
    User? Get(Guid id);
    User? GetByToken(string token);
}
