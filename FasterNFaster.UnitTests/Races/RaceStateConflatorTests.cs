using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Realtime.RaceState;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FasterNFaster.UnitTests.Races;

public class RaceStateConflatorTests
{
    private static IReadOnlyList<RaceParticipantDTO> Frame() =>
        [new(Guid.NewGuid(), 0, 0)];

    [Fact]
    public async Task DropsStaleFrames_SendsLatestWhilePreviousInFlight()
    {
        var broadcaster = new GatedBroadcaster();
        var conflator = new RaceStateConflator(broadcaster, NullLogger<RaceStateConflator>.Instance);
        var lobbyId = Guid.NewGuid();

        var first = Frame();
        var stale = Frame();
        var latest = Frame();

        conflator.Publish(lobbyId, [], first);
        await broadcaster.WaitForSendStarted();

        conflator.Publish(lobbyId, [], stale);
        conflator.Publish(lobbyId, [], latest);

        broadcaster.CompleteSend();
        await broadcaster.WaitForSendStarted();
        broadcaster.CompleteSend();

        Assert.Collection(broadcaster.Sent,
            s => Assert.Same(first, s),
            s => Assert.Same(latest, s));
    }

    [Fact]
    public async Task PublishAfterDrain_StartsNewSend()
    {
        var broadcaster = new GatedBroadcaster();
        var conflator = new RaceStateConflator(broadcaster, NullLogger<RaceStateConflator>.Instance);
        var lobbyId = Guid.NewGuid();

        var first = Frame();
        var second = Frame();

        conflator.Publish(lobbyId, [], first);
        await broadcaster.WaitForSendStarted();
        broadcaster.CompleteSend();

        conflator.Publish(lobbyId, [], second);
        await broadcaster.WaitForSendStarted();
        broadcaster.CompleteSend();

        Assert.Collection(broadcaster.Sent,
            s => Assert.Same(first, s),
            s => Assert.Same(second, s));
    }

    [Fact]
    public void Prune_RemovesLobbiesNotInActiveSet()
    {
        var broadcaster = new FakeBroadcaster();
        var conflator = new RaceStateConflator(broadcaster, NullLogger<RaceStateConflator>.Instance);
        var kept = Guid.NewGuid();
        var removed = Guid.NewGuid();

        conflator.Publish(kept, [], Frame());
        conflator.Publish(removed, [], Frame());

        conflator.Prune(new HashSet<Guid> { kept });

        Assert.Equal(1, conflator.TrackedLobbies);
    }
}
