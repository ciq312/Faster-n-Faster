using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Domain.Exceptions;
using FasterNFaster.Api.UseCases.Users.Services;
using FasterNFaster.UnitTests.Fakes;

namespace FasterNFaster.UnitTests.Users;

public class UserProfileServiceTests
{
    private readonly FakeUserRepository users = new();
    private readonly FakeStatisticsRepository stats = new();
    private readonly UserProfileService service;

    public UserProfileServiceTests() =>
        service = new UserProfileService(users, stats, new FakeLogger<UserProfileService>());

    [Fact]
    public async Task RegisteredUserWithoutStats_CreatesStatsAndRegistersRace()
    {
        var user = SeedUser();

        await service.ProcessRaceResultsAsync([Result(user.Id, wpm: 80, finishPosition: 1)]);

        var created = await stats.FindAsync(user.Id);
        Assert.NotNull(created);
        Assert.Equal(1, created.RacesTyped);
        Assert.Equal(1, created.Wins);
        Assert.Equal(80, created.BestWPM);
        Assert.Equal(1, stats.SaveCalls);
    }

    [Fact]
    public async Task RegisteredUserWithStats_AccumulatesRace()
    {
        var user = SeedUser();
        var existing = new PlayerStatistics(user.Id);
        existing.RegisterRace(Result(user.Id, wpm: 60, finishPosition: 2));
        stats.Seed(existing);

        await service.ProcessRaceResultsAsync([Result(user.Id, wpm: 90, finishPosition: 1)]);

        Assert.Equal(2, existing.RacesTyped);
        Assert.Equal(1, existing.Wins);
        Assert.Equal(90, existing.BestWPM);
        Assert.Equal(75, existing.AvgWPM);
    }

    [Fact]
    public async Task GuestResult_IsSkipped()
    {
        var guestId = Guid.NewGuid();

        await service.ProcessRaceResultsAsync([Result(guestId, wpm: 80, finishPosition: 1)]);

        Assert.Equal(0, stats.FindCalls);
        Assert.Null(await stats.FindAsync(guestId));
    }

    [Fact]
    public async Task RegisteredUser_ReturnsStatistics()
    {
        var user = SeedUser();
        var existing = new PlayerStatistics(user.Id);
        stats.Seed(existing);

        var profile = await service.GetProfileAsync(user.Id);

        Assert.Same(existing, profile);
    }

    [Fact]
    public async Task UnknownUserProfile_ThrowsUserNotFound()
    {
        await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetProfileAsync(Guid.NewGuid()));

        Assert.Equal(0, stats.GetByUserIdCalls);
    }

    private User SeedUser()
    {
        var user = new User("alice");
        users.Seed(user);
        return user;
    }

    private static RaceParticipantResult Result(Guid playerId, float wpm, int finishPosition) =>
        new(Guid.NewGuid(), playerId, "alice", wpm, 95, 1, 10, finishPosition, DateTimeOffset.UnixEpoch);
}
