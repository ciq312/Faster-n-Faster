using System.Runtime.CompilerServices;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.Web.Lobbies.LobbyState;
using static FasterNFaster.Api.Core.Entities.Lobbies.Races.WordRace;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public class RefreshPassageHandler(
    IPassageProvider passageProvider,
    ILobbyStore store,
    ILobbyService lobbyService,
    ILobbySessionService lobbySessionService,
    IRaceService raceService,
    LobbyStateBroadcaster broadcaster) : IHandler<RefreshPassageCommand>
{
    private readonly IPassageProvider passageProvider = passageProvider;
    private readonly ILobbyStore store = store;
    private readonly IRaceService raceService = raceService;
    private readonly ILobbyService lobbyService = lobbyService;
    private readonly ILobbySessionService lobbySessionService = lobbySessionService;
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;

    public async Task Handle(RefreshPassageCommand command)
    {
        var lobby = store.Get(command.LobbyId) ?? throw new LobbyNotFoundException(command.LobbyId);

        await lobbySessionService.RefreshPassage(command.CallerId);

        await broadcaster.BroadcastLobbyState(lobby);
    }
}
