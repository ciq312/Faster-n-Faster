using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races.Events;

namespace FasterNFaster.Tests.Services;

public class RaceAccessTests
{
    [Fact]
    public async Task TryMutateOnRemovedRace_ReturnsFalse()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));
        context.RaceAccess.Remove(context.LobbyId);

        var mutated = await context.RaceAccess.TryMutate(context.LobbyId, _ => { });

        Assert.False(mutated);
    }

    [Fact]
    public async Task TryMutateOnExistingRace_MutatesAndReturnsTrue()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));
        var mutateCalled = false;

        var mutated = await context.RaceAccess.TryMutate(context.LobbyId, _ => mutateCalled = true);

        Assert.True(mutated);
        Assert.True(mutateCalled);
    }

    [Fact]
    public async Task TryMutateOnExistingRace_DispatchesDrainedEvents()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(context, host.Id);

        await context.RaceAccess.TryMutate(context.LobbyId, r => r.WithdrawParticipant(host.Id));

        Assert.Single(context.Dispatcher.Dispatched.OfType<RaceFinishedEvent>());
    }

    [Fact]
    public async Task SnapshotOfRemovedRace_IsNull()
    {
        var context = await LobbyFactory.WithPlayers(new User("host"));
        context.RaceAccess.Remove(context.LobbyId);

        var snapshot = await context.RaceAccess.GetSnapshotOrDefault(context.LobbyId);

        Assert.Null(snapshot);
    }

    [Fact]
    public async Task ProcessUpdateOnRemovedRace_DoesNotThrow()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        context.RaceAccess.Remove(context.LobbyId);

        var exception = await Record.ExceptionAsync(() => context.RaceAccess.ProcessUpdate(context.LobbyId, host.Id, 1, 0, "a"));

        Assert.Null(exception);
    }
}
