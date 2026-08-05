using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Entities.Races;
using FasterNFaster.Api.UseCases.Interfaces.Users;

namespace FasterNFaster.Tests.Fakes;

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
