using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.UseCases.Lobbies.Services;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Lobbies;

public class TrackingLobbyStoreTests
{
    [Fact]
    public async Task SuccessfulMutate_MarksLobby()
    {
        var (store, lobbyId, tracker) = await StoreWithLobby();

        var changed = await Changed(tracker, () => store.Mutate(lobbyId, _ => { }));

        Assert.Equal([lobbyId], changed);
    }

    [Fact]
    public async Task ExistingLobbyTryMutate_MarksLobby()
    {
        var (store, lobbyId, tracker) = await StoreWithLobby();

        var changed = await Changed(tracker, () => store.TryMutate(lobbyId, _ => { }));

        Assert.Equal([lobbyId], changed);
    }

    [Fact]
    public async Task MissingLobbyTryMutate_DoesNotMark()
    {
        var (store, _, tracker) = await StoreWithLobby();
        var mutated = true;

        var changed = await Changed(tracker, async () => mutated = await store.TryMutate(Guid.NewGuid(), _ => { }));

        Assert.False(mutated);
        Assert.Empty(changed);
    }

    [Fact]
    public async Task ThrowingMutation_DoesNotMark()
    {
        var (store, lobbyId, tracker) = await StoreWithLobby();

        var changed = await Changed(tracker, async () =>
            await Assert.ThrowsAsync<InvalidOperationException>(() => store.Mutate(lobbyId, l => l.EndSession())));

        Assert.Empty(changed);
    }

    [Fact]
    public async Task ReadsAndCreate_DoNotMark()
    {
        var (store, lobbyId, tracker) = await StoreWithLobby();

        var changed = await Changed(tracker, async () =>
        {
            await store.Create("Other", false, Guid.NewGuid());
            await store.Read(lobbyId, l => l.Id);
            await store.TryRead(lobbyId, l => l.Id);
            await store.RemoveIfEmpty(lobbyId);
            store.TryClaimPlayer(Guid.NewGuid(), lobbyId);
        });

        Assert.Empty(changed);
    }

    private static async Task<(TrackingLobbyStore Store, Guid LobbyId, LobbyStateTracker Tracker)> StoreWithLobby()
    {
        var tracker = new LobbyStateTracker();
        var store = new TrackingLobbyStore(new InMemoryLobbyStore(new FakeEventDispatcher()), tracker);
        var (lobbyId, _) = await store.Create("Test", false, Guid.NewGuid());

        return (store, lobbyId, tracker);
    }

    private static async Task<IReadOnlyCollection<Guid>> Changed(LobbyStateTracker tracker, Func<Task> operation)
    {
        tracker.TryBeginScope();
        await operation();

        return tracker.EndScope();
    }
}
