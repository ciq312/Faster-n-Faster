namespace FasterNFaster.Api.UseCases.Interfaces.Db;

public interface IUnitOfWork
{
    Task SaveChangesAsync();
}