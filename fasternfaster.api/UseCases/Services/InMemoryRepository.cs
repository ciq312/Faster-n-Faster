using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Interfaces;

namespace FasterNFaster.Api.Infrastructure.Store;

public class InMemoryRepository<T> : IRepository<T> where T : class, IEntity
{
    private readonly ConcurrentDictionary<Guid, T> _items = new();

    public void Add(T entity) => _items[entity.Id] = entity;

    public T? Get(Guid id) => _items.GetValueOrDefault(id);

    public IReadOnlyCollection<T> GetAll() => _items.Values.ToList();
}
