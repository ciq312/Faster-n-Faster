using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace FasterNFaster.Tests.Services;

public class RaceStateConflatorTests
{
    private static IReadOnlyList<ParticipantSnapshot> Frame(string tag) =>
        new List<ParticipantSnapshot> { new(Guid.NewGuid(), 0, tag, 0, "red", tag, 0) };

    [Fact]
    public async Task DropsStaleFrames_SendsLatestWhilePreviousInFlight()
    {
        var broadcaster = new GatedRaceBroadcaster();
        var conflator = new RaceStateConflator(broadcaster, NullLogger<RaceStateConflator>.Instance);
        var lobbyId = Guid.NewGuid();

        var first = Frame("first");
        var stale = Frame("stale");
        var latest = Frame("latest");

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
        var broadcaster = new GatedRaceBroadcaster();
        var conflator = new RaceStateConflator(broadcaster, NullLogger<RaceStateConflator>.Instance);
        var lobbyId = Guid.NewGuid();

        var first = Frame("first");
        var second = Frame("second");

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
        var broadcaster = new GatedRaceBroadcaster();
        var conflator = new RaceStateConflator(broadcaster, NullLogger<RaceStateConflator>.Instance);
        var kept = Guid.NewGuid();
        var removed = Guid.NewGuid();

        conflator.Publish(kept, [], Frame("a"));
        conflator.Publish(removed, [], Frame("b"));

        conflator.Prune(new HashSet<Guid> { kept });

        Assert.Equal(1, conflator.TrackedLobbies);
    }
}
