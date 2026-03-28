using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Infrastructure;

namespace FasterNFaster.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    // добавляешь юзеров заранее чтобы симулировать "уже есть в бд"
    public void Seed(User user) => _users.Add(user);

    public Task AddAsync(User user)
    {
        _users.Add(user);
        return Task.CompletedTask;
    }

    public Task<User?> GetByIdAsync(Guid id)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByTokenAsync(string token)
        => Task.FromResult(_users.FirstOrDefault(u => u.Token == token));

    public Task<bool> DoUserExistByLoginAsync(string login)
        => Task.FromResult(_users.Any(u => u.Login == login));

    public Task<bool> DoUserExistByNickAsync(string nick)
        => Task.FromResult(_users.Any(u => u.Nick == nick));

    public Task<User?> GetUserByLoginAsync(string login)
        => Task.FromResult(_users.FirstOrDefault(u => u.Login == login));
}
