using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Exceptions;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public class UpdateProgressHandler(ILobbyStore lobbyStore, IEventDispatcher eventDispatcher) : IHandler<UpdateProgressCommand>
{
    private readonly ILobbyStore lobbyStore = lobbyStore;
    private readonly IEventDispatcher eventDispatcher = eventDispatcher;

    public async Task Handle(UpdateProgressCommand command)
    {
        var lobby = lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        if (!lobby.IsSessionActive)
            throw new InvalidOperationException("Race is not active.");

        var race = lobby.Race
            ?? throw new InvalidOperationException("Race not configured.");

        var participant = race.Participants.GetValueOrDefault(command.UserId) ?? throw new UserNotFoundException(command.UserId);

        if (participant.IsFinished) return;

        race.ProcessUpdate(command.UserId, command.Index, command.Mistakes);

        if (participant.IsFinished)
        {
            await eventDispatcher.Dispatch(new PlayerFinishedEvent(
                participant.Nick,
                command.LobbyId,
                command.UserId,
                participant.Result!.FinishPosition,
                participant.Result.WPM,
                participant.Result.Accuracy));
        }
    }
}
