using FasterNFaster.Api.Core.Entities;
using Microsoft.Extensions.Time.Testing;

namespace FasterNFaster.Tests.Services;

public class EmptyLobbySweepServiceTests
{
    private static readonly TimeSpan Ttl = LobbyFactory.EmptyLobbyTtl;

    [Fact]
    public async Task EmptyLobbyPastTtl_SweepRemovesLobbyAndRace()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sweep = LobbyFactory.SweepService(context, time);
        time.Advance(Ttl);

        await sweep.Sweep(CancellationToken.None);

        Assert.Null(context.Store.Get(context.LobbyId));
        Assert.Null(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
    }

    [Fact]
    public async Task EmptyLobbyYoungerThanTtl_SweepKeepsLobby()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sweep = LobbyFactory.SweepService(context, time);
        time.Advance(Ttl - TimeSpan.FromSeconds(5));

        await sweep.Sweep(CancellationToken.None);

        Assert.NotNull(context.Store.Get(context.LobbyId));
        Assert.NotNull(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
    }

    [Fact]
    public async Task OldLobbyWithPlayers_SweepKeepsLobby()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sweep = LobbyFactory.SweepService(context, time);
        time.Advance(Ttl * 10);

        await sweep.Sweep(CancellationToken.None);

        Assert.NotNull(context.Store.Get(context.LobbyId));
        Assert.NotNull(await context.RaceAccess.GetRaceSettingsOrDefault(context.LobbyId));
    }

    [Fact]
    public async Task CancelledSweep_RemovesNothing()
    {
        var context = await LobbyFactory.Empty(Guid.NewGuid());
        var time = new FakeTimeProvider(DateTimeOffset.UtcNow);
        var sweep = LobbyFactory.SweepService(context, time);
        time.Advance(Ttl);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        await sweep.Sweep(cts.Token);

        Assert.NotNull(context.Store.Get(context.LobbyId));
    }
}
