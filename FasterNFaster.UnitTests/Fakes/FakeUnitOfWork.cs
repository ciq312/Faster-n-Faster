using FasterNFaster.Api.UseCases.Shared;

namespace FasterNFaster.UnitTests.Fakes;

public class FakeUnitOfWork : IUnitOfWork
{
    public Task SaveChangesAsync()
    {
        return Task.CompletedTask;
    }
}
