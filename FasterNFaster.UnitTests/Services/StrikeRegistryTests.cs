using FasterNFaster.Api.Infrastructure.Users;

namespace FasterNFaster.Tests.Services;

public class StrikeRegistryTests
{
    [Fact]
    public void RecordRaceViolation_CountsPerLobbyAndPlayer()
    {
        var registry = new StrikeRegistry();
        var lobbyId = Guid.NewGuid();
        var playerId = Guid.NewGuid();

        Assert.Equal(1, registry.RecordRaceViolation(lobbyId, playerId));
        Assert.Equal(2, registry.RecordRaceViolation(lobbyId, playerId));
    }

    [Fact]
    public void RecordRaceViolation_DifferentPlayersInSameLobby_CountSeparately()
    {
        var registry = new StrikeRegistry();
        var lobbyId = Guid.NewGuid();
        var playerA = Guid.NewGuid();
        var playerB = Guid.NewGuid();

        registry.RecordRaceViolation(lobbyId, playerA);
        registry.RecordRaceViolation(lobbyId, playerA);

        Assert.Equal(1, registry.RecordRaceViolation(lobbyId, playerB));
    }

    [Fact]
    public void ClearRace_ResetsCountForThatLobbyOnly()
    {
        var registry = new StrikeRegistry();
        var lobbyA = Guid.NewGuid();
        var lobbyB = Guid.NewGuid();
        var playerId = Guid.NewGuid();
        registry.RecordRaceViolation(lobbyA, playerId);
        registry.RecordRaceViolation(lobbyB, playerId);

        registry.ClearRace(lobbyA);

        Assert.Equal(1, registry.RecordRaceViolation(lobbyA, playerId));
        Assert.Equal(2, registry.RecordRaceViolation(lobbyB, playerId));
    }

    [Fact]
    public void RecordStrike_WithinWindow_Accumulates()
    {
        var registry = new StrikeRegistry();
        var playerId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var window = TimeSpan.FromHours(24);

        registry.RecordStrike(playerId, now, window);
        var count = registry.RecordStrike(playerId, now.AddHours(1), window);

        Assert.Equal(2, count);
    }

    [Fact]
    public void RecordStrike_OutsideWindow_IsPruned()
    {
        var registry = new StrikeRegistry();
        var playerId = Guid.NewGuid();
        var now = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var window = TimeSpan.FromHours(24);

        registry.RecordStrike(playerId, now, window);
        var count = registry.RecordStrike(playerId, now.AddHours(25), window);

        Assert.Equal(1, count);
    }
}
