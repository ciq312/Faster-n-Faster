using FasterNFaster.Api.UseCases.Interfaces.Db;

namespace FasterNFaster.Tests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }
}