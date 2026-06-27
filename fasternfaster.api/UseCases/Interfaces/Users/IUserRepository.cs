using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Lobbies;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IUserRepository
{
    Task AddAsync(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task<bool> IsUserRegistred(Guid id);
    Task<bool> DoUserExistByNickAsync(string nick);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetUserByLoginAsync(string login);
}
