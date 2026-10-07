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
        var events = await Read(key, MutateAndDrain(mutate));

        await Dispatch(events);
    }

    public async Task<bool> TryMutate(Guid key, Func<Guid, TAggregate?> tryResolve, Action<TAggregate> mutate)
    {
        var events = await TryRead(key, tryResolve, MutateAndDrain(mutate));
        if (events is null) return false;

        await Dispatch(events);
        return true;
    }

    public Task<T> Read<T>(Guid key, Func<TAggregate, T> read) =>
        WithGate(key, gate => read(Resolve(key, gate)));

    public Task<T?> TryRead<T>(Guid key, Func<Guid, TAggregate?> tryResolve, Func<TAggregate, T> read) =>
        WithGate<T?>(key, gate =>
        {
            var aggregate = tryResolve(key);
            if (aggregate is not null) return read(aggregate);

            RemoveGate(key, gate);
            return default;
        });

    public Task DispatchOf(TAggregate aggregate) => Dispatch(aggregate.DrainEvents());

    // The gate is deliberately not disposed: concurrent callers may still Wait/Release on it.
    // SemaphoreSlim owns no OS handle here, so GC collects it; disposing would fault or hang them.
    public void Release(Guid key) => gates.TryRemove(key, out _);

    private async Task<T> WithGate<T>(Guid key, Func<SemaphoreSlim, T> body)
    {
        var gate = gates.GetOrAdd(key, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync();
        try
        {
            return body(gate);
        }
        finally
        {
            gate.Release();
        }
    }

    private TAggregate Resolve(Guid key, SemaphoreSlim gate)
    {
        try
        {
            return resolve(key);
        }
        catch
        {
            RemoveGate(key, gate);
            throw;
        }
    }

    // Safe only because an aggregate never appears for a key after a lookup missed it,
    // except right after lobby creation, when only read-only lookups can run.
    private void RemoveGate(Guid key, SemaphoreSlim gate) =>
        gates.TryRemove(new KeyValuePair<Guid, SemaphoreSlim>(key, gate));

    private static Func<TAggregate, IReadOnlyList<IDomainEvent>> MutateAndDrain(Action<TAggregate> mutate) =>
        aggregate =>
        {
            mutate(aggregate);
            return aggregate.DrainEvents();
        };

    private async Task Dispatch(IReadOnlyList<IDomainEvent> events)
    {
        foreach (var domainEvent in events)
            await dispatcher.Dispatch(domainEvent, CancellationToken.None);
    }
}
