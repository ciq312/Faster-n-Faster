using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Entities.Races.Events;

namespace FasterNFaster.UnitTests.Races;

public class InMemoryRaceStoreTests
{
    [Fact]
    public async Task RemovedRace_TryMutateReturnsFalse()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));
        context.Races.Remove(context.LobbyId);

        var mutated = await context.Races.TryMutate(context.LobbyId, _ => { });

        Assert.False(mutated);
    }

    [Fact]
    public async Task ExistingRace_TryMutateMutatesAndReturnsTrue()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));
        var mutateCalled = false;

        var mutated = await context.Races.TryMutate(context.LobbyId, _ => mutateCalled = true);

        Assert.True(mutated);
        Assert.True(mutateCalled);
    }

    [Fact]
    public async Task RaceFinishingMutation_DispatchesDrainedEvents()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(context, host.Id);

        await context.Races.TryMutate(context.LobbyId, r => r.WithdrawParticipant(host.Id));

        Assert.Single(context.Dispatcher.Dispatched.OfType<RaceFinishedEvent>());
    }

    [Fact]
    public async Task RemovedRace_TryReadReturnsDefault()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));
        context.Races.Remove(context.LobbyId);

        var snapshot = await context.Races.TryRead(context.LobbyId, r => r.GetSnapshot());

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task MissingRace_ReadThrowsInvalidOperation()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));
        context.Races.Remove(context.LobbyId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.ReadRace(r => r.WordCount));
    }

    [Fact]
    public async Task AddedAndRemovedRace_ExistsReflectsBoth()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        Assert.True(context.Races.Exists(context.LobbyId));

        context.Races.Remove(context.LobbyId);
        Assert.False(context.Races.Exists(context.LobbyId));

        context.Races.Add(new Race(context.LobbyId, 5, "one two three four five"));
        Assert.True(context.Races.Exists(context.LobbyId));
    }
}
