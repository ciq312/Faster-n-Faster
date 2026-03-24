using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Infrastructure;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetAsync(Guid id);
    Task<User?> GetByTokenAsync(string token);
}
