using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Tests.Fakes;

public class FakeUserRepository : IUserRepository
{
    private readonly List<User> _users = new();

    public IReadOnlyList<User> Users => _users;

    public void Seed(User user) => _users.Add(user);

    public void Add(User user)
    {
        _users.Add(user);
    }

    public void Update(User user)
    {
        var existingUser = _users.FirstOrDefault(u => u.Id == user.Id);
        if (existingUser != null)
        {
            _users.Remove(existingUser);
            _users.Add(user);
        }
    }

    public Task<User?> GetByIdAsync(Guid id)
        => Task.FromResult(_users.FirstOrDefault(u => u.Id == id));

    public Task<User?> GetByEmailAsync(string email) => Task.FromResult(_users.FirstOrDefault(x => x.Email == email));
    public Task<User?> GetUserByLoginAsync(string login)
        => Task.FromResult(_users.FirstOrDefault(u => u.Login == login));

    public Task<bool> IsUserRegistred(Guid id) => Task.FromResult(Users.Any(u => u.Id == id));
}
