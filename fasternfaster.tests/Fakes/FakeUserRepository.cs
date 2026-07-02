using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public IReadOnlyList<User> Users => _users;

    public void Seed(User user) => _users.Add(user);

    public Task AddAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task UpdateAsync(User user) => Task.CompletedTask;

    public Task<User?> GetByIdAsync(Guid id)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email) => Task.FromResult(_users.FirstOrDefault(x => x.Email == email));
    public Task<bool> DoUserExistByLoginAsync(string login)
        => Task.FromResult(_users.Any(u => u.Login == login));

    public Task<bool> DoUserExistByNickAsync(string nick)
        => Task.FromResult(_users.Any(u => u.Nick == nick));

    public Task<User?> GetUserByLoginAsync(string login)
        => Task.FromResult(_users.FirstOrDefault(u => u.Login == login));

    public Task<bool> IsUserRegistred(Guid id) => Task.FromResult(Users.Any(u => u.Id == id));
}
