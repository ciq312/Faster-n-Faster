using FasterNFaster.Api.Core.Entities.Lobbies.Races;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Interfaces.Users;
using FasterNFaster.Tests.Fakes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace FasterNFaster.Tests.Services;

public class RaceResultProcessorTests
{
    [Fact]
    public async Task Drains_Queue_And_Persists_Results()
    {
        var profileService = new FakeUserProfileService();
        var services = new ServiceCollection();
        services.AddScoped<IUserProfileService>(_ => profileService);
        using var provider = services.BuildServiceProvider();

        var queue = new RaceResultQueue();
        var processor = new RaceResultProcessor(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RaceResultProcessor>.Instance);

        var results = new List<RaceParticipantResult>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "nick", 80f, 95f, 1, 10, 1)
        };
        queue.Enqueue(results);

        await processor.StartAsync(CancellationToken.None);
        var processed = await profileService.Processed.WaitAsync(TimeSpan.FromSeconds(5));
        await processor.StopAsync(CancellationToken.None);

        Assert.Same(results, processed);
    }
}
