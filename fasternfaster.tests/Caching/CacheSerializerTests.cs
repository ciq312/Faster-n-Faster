using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.Infrastructure.Caching;

namespace FasterNFaster.Tests.Caching;

public class CacheSerializerTests
{
    [Fact]
    public void RoundTrip_PlayerStatistics_PreservesPrivateSetterFields()
    {
        var stats = new PlayerStatistics(Guid.NewGuid());
        stats.RegisterRace(new RaceParticipantResult(Guid.NewGuid(), stats.Id, "Nick", 60, 90, 0, 100, 1));

        var restored = CacheSerializer.Deserialize<PlayerStatistics>(CacheSerializer.Serialize(stats))!;

        Assert.Equal(stats.Id, restored.Id);
        Assert.Equal(stats.BestWPM, restored.BestWPM);
        Assert.Equal(stats.AvgWPM, restored.AvgWPM);
        Assert.Equal(stats.BestAccuracy, restored.BestAccuracy);
        Assert.Equal(stats.Wins, restored.Wins);
        Assert.Equal(stats.WordsTyped, restored.WordsTyped);
        Assert.Equal(stats.RacesTyped, restored.RacesTyped);
    }

    [Fact]
    public void RoundTrip_User_BypassesValidatingConstructor_AndPreservesIdentity()
    {
        var user = new User("Alice", "alice", "hashed-password");
        user.SetEmail("alice@example.com");
        user.SetEmailVerified();

        var restored = CacheSerializer.Deserialize<User>(CacheSerializer.Serialize(user))!;

        // A regenerated Id would prove the constructor ran; equality proves it was bypassed.
        Assert.Equal(user.Id, restored.Id);
        Assert.Equal(user.Nick, restored.Nick);
        Assert.Equal(user.Login, restored.Login);
        Assert.Equal(user.Email, restored.Email);
        Assert.True(restored.IsEmailVerified);
    }
}
