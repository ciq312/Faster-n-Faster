using FasterNFaster.Api.Domain.Entities;

namespace FasterNFaster.Api.UseCases.Interfaces.Users;

public interface IUserRepository
{
    void Add(User user);
    void Update(User user);
    Task<User?> GetByIdAsync(Guid id);
    Task<bool> IsUserRegistered(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetUserByLoginAsync(string login);
}
