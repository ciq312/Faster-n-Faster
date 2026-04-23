using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.Infrastructure;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task<bool> DoUserExistByNickAsync(string nick);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetUserByLoginAsync(string login);
}
