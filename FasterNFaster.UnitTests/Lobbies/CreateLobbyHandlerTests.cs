using FasterNFaster.Api.Domain.Entities;
using FasterNFaster.Api.Domain.Exceptions.Lobbies;
using FasterNFaster.Api.Infrastructure.Lobbies;
using FasterNFaster.Api.Infrastructure.Races;
using FasterNFaster.Api.UseCases.Lobbies.CreateLobby;
using FasterNFaster.UnitTests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using Shouldly;

namespace FasterNFaster.UnitTests.Lobbies;

public class CreateLobbyHandlerTests
{
    [Fact]
    public async Task CreateLobby_ShouldReturnLobbyIdAndName()
    {
        var (createLobbyHandler, lobbies) = Build();
        var hostId = Guid.NewGuid();

        var result = await createLobbyHandler.Handle(new CreateLobbyCommand("testLobby", false, hostId), CancellationToken.None);

        var (name, storedHostId) = await lobbies.Read(result.LobbyId, l => (l.Name, l.HostId));

        name.ShouldBe("testLobby");
        storedHostId.ShouldBe(hostId);
    }

    [Fact]
    public async Task CreateLobby_ShouldStoreTheLobby()
    {
        var (createLobbyHandler, lobbies) = Build();

        await createLobbyHandler.Handle(new CreateLobbyCommand("testLobby", false, Guid.NewGuid()), CancellationToken.None);

        lobbies.GetAllIds().ShouldHaveSingleItem();
    }

    [Fact]
    public async Task CreatorAlreadyInLobby_ThrowsAlreadyInLobby()
    {
        var host = new User("host");
        var context = await LobbyFactory.WithPlayers(host);
        var handler = new CreateLobbyHandler(new RandomPassageProvider(), context.Lobbies, context.Races, context.Time);

        await Should.ThrowAsync<AlreadyInLobbyException>(() =>
            handler.Handle(new CreateLobbyCommand("second", false, host.Id), CancellationToken.None));

        context.Lobbies.GetAllIds().ShouldHaveSingleItem();
    }

    private static (CreateLobbyHandler Handler, InMemoryLobbyStore Lobbies) Build()
    {
        var dispatcher = new FakeEventDispatcher();
        var time = new FakeTimeProvider();
        var lobbies = new InMemoryLobbyStore(dispatcher, time);
        var races = new InMemoryRaceStore(dispatcher, NullLogger<InMemoryRaceStore>.Instance);

        return (new CreateLobbyHandler(new RandomPassageProvider(), lobbies, races, time), lobbies);
    }
}
