using FasterNFaster.Api.Core.Entities;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;
using FasterNFaster.Api.UseCases.Interfaces.Lobbies;

namespace FasterNFaster.Api.UseCases.Lobbies.TransferHost;

public class TransferHostHandler(ILobbyService lobbyService) : IHandler<TransferHostCommand>
{
    private readonly ILobbyService lobbyService = lobbyService;
    public async Task Handle(TransferHostCommand command)
    {
        await lobbyService.TransferHost(command.HostId, command.UserId);
    }
}
