using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Infrastructure.Shared.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FasterNFaster.IntegrationTests;

public static class StatisticsSeeder
{
    public static RaceParticipantResult Race(float wpm = 30, float accuracy = 60, int wordsTyped = 10, int? finishPosition = 2) =>
        new(Guid.NewGuid(), Guid.Empty, "racer", wpm, accuracy, 0, wordsTyped, finishPosition, TimeProvider.System.GetUtcNow());

    public static Task SeedStatisticsAsync(
        this WebApplicationFactory<Program> app, Guid userId, params RaceParticipantResult[] races) =>
        app.ExecuteScopedAsync<AppDbContext>(async context =>
        {
            context.Statistics.Add(NewStatistics(userId, races));
            await context.SaveChangesAsync();
        });

    public static Task<Guid> SeedPlayerAsync(
        this WebApplicationFactory<Program> app, string nick, params RaceParticipantResult[] races) =>
        app.ExecuteScopedAsync<AppDbContext, Guid>(async context =>
        {
            var user = new User(nick);
            context.Users.Add(user);
            context.Statistics.Add(NewStatistics(user.Id, races));
            await context.SaveChangesAsync();
            return user.Id;
        });

    private static PlayerStatistics NewStatistics(Guid userId, RaceParticipantResult[] races)
    {
        var statistics = new PlayerStatistics(userId);
        foreach (var race in races)
            statistics.RegisterRace(race);

        return statistics;
    }
}
