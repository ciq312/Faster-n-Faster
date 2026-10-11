using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Entities.Races;
using FasterNFaster.Api.UseCases.Users.Services;

namespace FasterNFaster.UnitTests.Fakes;

public class FakeUserProfileService : IUserProfileService
{
    private readonly TaskCompletionSource<IEnumerable<RaceParticipantResult>> processed =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<IEnumerable<RaceParticipantResult>> Processed => processed.Task;

    public Task<PlayerStatistics?> GetProfileAsync(Guid userId) =>
        Task.FromResult<PlayerStatistics?>(null);

    public Task ProcessRaceResultsAsync(IEnumerable<RaceParticipantResult> results)
    {
        processed.TrySetResult(results);
        return Task.CompletedTask;
    }
}
