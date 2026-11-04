using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.UseCases.Races.GetRaceProgress;
using Shouldly;
using static FasterNFaster.UnitTests.Shared.KeystrokeHelper;

namespace FasterNFaster.UnitTests.Races;

public class GetRaceProgressHandlerTests
{
    [Fact]
    public async Task StartedRace_ReturnsCallersProgress()
    {
        var host = new User("host");
        var context = await StartedSoloRace(host);
        var passage = await context.ReadRace(r => r.Passage);
        await context.Races.Mutate(context.LobbyId, r => r.ProcessUpdate(host.Id, [CharTyped(passage[0]), CharTyped('#')]));

        var progress = await GetProgress(context, host.Id);

        progress.ShouldBe(new RaceProgressDTO(LastValidIndex: 0, WrongTail: "#", Mistakes: 1));
    }

    [Fact]
    public async Task RaceNotStarted_ReturnsNull()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);

        var progress = await GetProgress(context, host.Id);

        progress.ShouldBeNull();
    }

    [Fact]
    public async Task PlayerOutsideLobby_ReturnsNull()
    {
        var host = new User("host");
        var context = await StartedSoloRace(host);

        var progress = await GetProgress(context, Guid.NewGuid());

        progress.ShouldBeNull();
    }

    private static async Task<LobbyTestContext> StartedSoloRace(User host)
    {
        var context = await LobbyFactory.WithPlayers(host);
        await LobbyFactory.StartRace(context, host.Id);
        await context.Races.Mutate(context.LobbyId, r => r.Start());
        return context;
    }

    private static Task<RaceProgressDTO?> GetProgress(LobbyTestContext context, Guid userId) =>
        new GetRaceProgressHandler(context.Races, context.Lobbies)
            .Handle(new GetRaceProgressQuery(userId), CancellationToken.None);
}
