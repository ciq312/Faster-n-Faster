using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Interfaces.Events;

namespace FasterNFaster.Api.UseCases.Services;

public sealed class AggregateGate<TAggregate>(IEventDispatcher dispatcher, Func<Guid, TAggregate> resolve)
    where TAggregate : AggregateRoot<Guid>
{
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> gates = new();

    public async Task Mutate(Guid key, Action<TAggregate> mutate)
    {
        var events = await Read(key, aggregate =>
        {
            mutate(aggregate);
            return aggregate.DrainEvents();
        });

        await Dispatch(events);
    }

    public async Task<T> Read<T>(Guid key, Func<TAggregate, T> read)
    {
        var gate = gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return read(resolve(key));
        }
        finally
        {
            gate.Release();
        }
    }

    public async Task<T?> TryRead<T>(Guid key, Func<Guid, TAggregate?> tryResolve, Func<TAggregate, T> read)
    {
        var gate = gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            var aggregate = tryResolve(key);
            return aggregate is null ? default : read(aggregate);
        }
        finally
        {
            gate.Release();
        }
    }

    public Task DispatchOf(TAggregate aggregate) => Dispatch(aggregate.DrainEvents());

    // The gate is deliberately not disposed: concurrent callers may still Wait/Release on it.
    // SemaphoreSlim owns no OS handle here, so GC collects it; disposing would fault or hang them.
    public void Release(Guid key) => gates.TryRemove(key, out _);

    private async Task Dispatch(IReadOnlyList<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
            await dispatcher.Dispatch(domainEvent, CancellationToken.None);
    }
}
