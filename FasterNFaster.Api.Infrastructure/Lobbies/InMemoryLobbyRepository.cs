using System.Collections.Concurrent;
using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using MediatR;

namespace FasterNFaster.Api.Infrastructure.Lobbies;

public class InMemoryLobbyRepository(IEventDispatcher mediator) : ILobbyRepository
{
    private readonly ConcurrentDictionary<Guid, Lobby> lobbies = new();
    private readonly IEventDispatcher mediator = mediator;
    private readonly List<Lobby> added = new List<Lobby>();
    private readonly List<Lobby> updated = new List<Lobby>();
    private readonly List<Lobby> removed = new List<Lobby>();


    public void Add(Lobby lobby)
    {
        added.Add(lobby);
    }

    public void Remove(Guid id)
    {
        removed.Add(lobbies.GetValueOrDefault(id) ?? throw new InvalidOperationException("Failed to remove lobby"));
    }

    public Lobby? Get(Guid id) => lobbies.GetValueOrDefault(id);

    public Lobby GetRequired(Guid id) =>
        lobbies.GetValueOrDefault(id) ?? throw new LobbyNotFoundException(id);

    public Lobby? GetByInviteCode(string code) =>
        lobbies.Values.FirstOrDefault(l =>
            l.LobbySettings.InviteCode != null &&
            l.LobbySettings.InviteCode.Equals(code, StringComparison.OrdinalIgnoreCase));

    public IReadOnlyCollection<Lobby> GetAll() => lobbies.Values.ToList();

    public void Update(Lobby lobby)
    {
        updated.Add(lobby);
    }

    public async Task SaveChanges()
    {
        foreach (var lobby in added)
            lobbies[lobby.Id] = lobby;

        foreach (var lobby in updated)
            lobbies[lobby.Id] = lobby;

        foreach (var lobby in removed)
            lobbies.TryRemove(lobby.Id, out _);

        var allAggregates = added.Concat(updated).Concat(removed).Distinct();
        var domainEvents = allAggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var lobby in allAggregates)
            lobby.ClearEvents();

        added.Clear();
        updated.Clear();
        removed.Clear();

        foreach (var e in domainEvents)
            await mediator.Dispatch(e, CancellationToken.None);
    }
}
