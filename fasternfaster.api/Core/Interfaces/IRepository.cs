namespace FasterNFaster.Api.Core.Interfaces;

public interface IRepository<T> where T : class, IEntity
{
    void Add(T entity);
    T? Get(Guid id);
    IReadOnlyCollection<T> GetAll();
}
