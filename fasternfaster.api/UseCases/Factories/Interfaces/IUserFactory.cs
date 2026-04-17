using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.UseCases.Factories.Interfaces;

public interface IUserFactory
{
    public Task<User> GetUser(Guid id, string nick, string role);
}