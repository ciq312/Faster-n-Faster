using System.Runtime.CompilerServices;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;
using FasterNFaster.Api.UseCases.Interfaces.Races;
using FasterNFaster.Api.Web.Lobbies.LobbyState;

namespace FasterNFaster.Api.UseCases.Lobbies.RefreshPassage;

public class RefreshPassageHandler(IPassageProvider passageProvider, ILobbyStore store, LobbyStateBroadcaster broadcaster) : IHandler<RefreshPassageCommand>
{
    private readonly IPassageProvider passageProvider = passageProvider;
    private readonly ILobbyStore store = store;
    private readonly LobbyStateBroadcaster broadcaster = broadcaster;
    public async Task Handle(RefreshPassageCommand command)
    {
        var lobby = store.Get(command.LobbyId) ?? throw new LobbyNotFoundException(command.LobbyId);
        lobby.ValidateHost(command.CallerId);
        var passage = await passageProvider.GetPassageAsync(lobby.Race.WordCount);
        lobby.Race.SetPassage(passage);

        await broadcaster.BroadcastLobbyState(lobby);

    }
}
