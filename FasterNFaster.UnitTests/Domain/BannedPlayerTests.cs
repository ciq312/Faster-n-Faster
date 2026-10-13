using FasterNFaster.Api.Domain.Entities;

namespace FasterNFaster.UnitTests.Domain;

public class BannedPlayerTests
{
    private static readonly DateTimeOffset BannedAt = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Later = BannedAt.AddHours(3);

    [Fact]
    public void NewBan_IsPermanentAndStampedAtGivenTime()
    {
        var userId = Guid.NewGuid();

        var ban = new BannedPlayer(userId, "cheating", BannedAt);

        Assert.Equal(userId, ban.UserId);
        Assert.Equal("cheating", ban.Reason);
        Assert.Equal(BannedAt, ban.BannedAt);
        Assert.Null(ban.ExpiresAt);
        Assert.Equal(0, ban.SuspensionCount);
    }

    [Fact]
    public void NewSuspension_ExpiresAtGivenTimeWithCountOne()
    {
        var expiresAt = BannedAt.AddHours(1);

        var ban = new BannedPlayer(Guid.NewGuid(), "cheating", expiresAt, BannedAt);

        Assert.Equal(BannedAt, ban.BannedAt);
        Assert.Equal(expiresAt, ban.ExpiresAt);
        Assert.Equal(1, ban.SuspensionCount);
    }

    [Fact]
    public void ExtendingActiveSuspension_MovesExpiryAndIncrementsCount()
    {
        var ban = new BannedPlayer(Guid.NewGuid(), "first", BannedAt.AddHours(1), BannedAt);
        var newExpiry = Later.AddHours(2);

        ban.ExtendSuspension("second", newExpiry, Later);

        Assert.Equal("second", ban.Reason);
        Assert.Equal(Later, ban.BannedAt);
        Assert.Equal(newExpiry, ban.ExpiresAt);
        Assert.Equal(2, ban.SuspensionCount);
    }

    [Fact]
    public void ExtendingPermanentBan_StaysPermanent()
    {
        var ban = new BannedPlayer(Guid.NewGuid(), "first", BannedAt);

        ban.ExtendSuspension("second", Later.AddHours(2), Later);

        Assert.Null(ban.ExpiresAt);
        Assert.Equal(0, ban.SuspensionCount);
    }

    [Fact]
    public void MakingSuspensionPermanent_ClearsExpiryAndRestamps()
    {
        var ban = new BannedPlayer(Guid.NewGuid(), "first", BannedAt.AddHours(1), BannedAt);

        ban.MakePermanent("final", Later);

        Assert.Null(ban.ExpiresAt);
        Assert.Equal("final", ban.Reason);
        Assert.Equal(Later, ban.BannedAt);
    }
}
