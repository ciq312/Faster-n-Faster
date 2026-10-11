namespace FasterNFaster.Api.UseCases.Shared;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}
