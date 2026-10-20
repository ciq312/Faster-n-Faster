using FasterNFaster.Api.Domain.Entities;

namespace FasterNFaster.UnitTests.Lobbies;

public class EmptyLobbySweepServiceTests
{
    private static readonly TimeSpan Ttl = LobbyFactory.EmptyLobbyTtl;

    [Fact]
    public async Task EmptyLobbyPastTtl_SweepRemovesLobbyAndRace()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var sweep = LobbyFactory.SweepService(context);
        context.Time.Advance(Ttl + TimeSpan.FromSeconds(1));

        await sweep.Sweep(CancellationToken.None);

        Assert.False(context.LobbyExists);
        Assert.False(context.Races.Exists(context.LobbyId));
    }

    [Fact]
    public async Task EmptyLobbyExactlyAtTtl_SweepRemovesLobby()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var sweep = LobbyFactory.SweepService(context);
        context.Time.Advance(Ttl);

        await sweep.Sweep(CancellationToken.None);

        Assert.False(context.LobbyExists);
    }

    [Fact]
    public async Task EmptyLobbyYoungerThanTtl_SweepKeepsLobby()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var sweep = LobbyFactory.SweepService(context);
        context.Time.Advance(Ttl - TimeSpan.FromTicks(1));

        await sweep.Sweep(CancellationToken.None);

        Assert.True(context.LobbyExists);
        Assert.True(context.Races.Exists(context.LobbyId));
    }

    [Fact]
    public async Task OldLobbyWithPlayers_SweepKeepsLobby()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        var sweep = LobbyFactory.SweepService(context);
        context.Time.Advance(Ttl * 10);

        await sweep.Sweep(CancellationToken.None);

        Assert.True(context.LobbyExists);
        Assert.True(context.Races.Exists(context.LobbyId));
    }

    [Fact]
    public async Task CancelledSweep_RemovesNothing()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var sweep = LobbyFactory.SweepService(context);
        context.Time.Advance(Ttl);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await sweep.Sweep(cts.Token);

        Assert.True(context.LobbyExists);
    }
}
