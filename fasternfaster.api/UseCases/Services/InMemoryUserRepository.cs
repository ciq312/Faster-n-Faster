using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.UseCases.Services;

public class InMemoryUserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<Guid, User> _users = new();

    public void Add(User user) => _users[user.Id] = user;

    public User? Get(Guid id) => _users.GetValueOrDefault(id);

    public User? GetByToken(string token) =>
        _users.Values.FirstOrDefault(u => u.Token == token);
}
