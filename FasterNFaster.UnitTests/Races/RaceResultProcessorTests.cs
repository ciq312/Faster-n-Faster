using System.Diagnostics.Metrics;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Shared;
using FasterNFaster.Api.UseCases.Users.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Shouldly;

namespace FasterNFaster.UnitTests.Races;

public class RaceResultProcessorTests
{
    [Theory]
    [InlineData(true, "persisted")]
    [InlineData(false, "failed")]
    public async Task QueuedResults_RecordOnePersistenceBatchOutcome(bool persisted, string expectedOutcome)
    {
        var measurement = new TaskCompletionSource<(long Count, string? Outcome)>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, current) =>
        {
            if (instrument.Meter.Name == RacePerformanceMetrics.MeterName && instrument.Name == "race.result.batches")
                current.EnableMeasurementEvents(instrument);
        };
        listener.SetMeasurementEventCallback<long>((_, count, tags, _) =>
        {
            if (count != 0)
                measurement.TrySetResult((count, (string?)tags[0].Value));
        });
        listener.Start();

        var results = new List<RaceParticipantResult>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "nick", 80f, 95f, 1, 10, 1, DateTimeOffset.UnixEpoch)
        };
        var profileService = new Mock<IUserProfileService>();
        profileService.Setup(service => service.ProcessRaceResultsAsync(results))
            .Returns(persisted ? Task.CompletedTask : Task.FromException(new InvalidOperationException()));
        var services = new ServiceCollection();
        services.AddScoped<IUserProfileService>(_ => profileService.Object);
        using var provider = services.BuildServiceProvider();

        var queue = new RaceResultQueue();
        using var processor = new RaceResultProcessor(
            queue,
            provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<RaceResultProcessor>.Instance);

        queue.Enqueue(results);

        await processor.StartAsync(CancellationToken.None);
        var recorded = await measurement.Task.WaitAsync(TimeSpan.FromSeconds(5));
        await processor.StopAsync(CancellationToken.None);

        profileService.Verify(service => service.ProcessRaceResultsAsync(results), Times.Once());
        recorded.Count.ShouldBe(1);
        recorded.Outcome.ShouldBe(expectedOutcome);
    }
}
