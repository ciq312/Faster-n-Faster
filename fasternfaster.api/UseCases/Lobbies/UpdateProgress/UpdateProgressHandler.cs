using FasterNFaster.Api.Core.Entities.Lobbies;
using FasterNFaster.Api.Core.Events;
using FasterNFaster.Api.Core.Interfaces;
using FasterNFaster.Api.Core.Interfaces.Events;
using FasterNFaster.Api.UseCases.Interfaces;

namespace FasterNFaster.Api.UseCases.Lobbies.UpdateProgress;

public class UpdateProgressHandler : IHandler<UpdateProgressCommand>
{
    private readonly ILobbyStore _lobbyStore;
    private readonly IRaceTickRegistry _raceTickRegistry;
    private readonly IEventDispatcher _eventDispatcher;

    public UpdateProgressHandler(ILobbyStore lobbyStore, IRaceTickRegistry raceTickRegistry, IEventDispatcher eventDispatcher)
    {
        _lobbyStore = lobbyStore;
        _raceTickRegistry = raceTickRegistry;
        _eventDispatcher = eventDispatcher;
    }

    public async Task Handle(UpdateProgressCommand command)
    {
        var lobby = _lobbyStore.Get(command.LobbyId)
            ?? throw new KeyNotFoundException("Lobby not found.");

        if (lobby.CurrentStatus != Lobby.Status.racing)
            throw new InvalidOperationException("Race is not active.");

        var race = lobby.Race
            ?? throw new InvalidOperationException("Race not configured.");

        var participant = race.ProcessUpdate(command.UserId, command.Index, command.Mistakes);
        if (participant == null)
            return;

        if (participant.IsFinished)
        {
            await _eventDispatcher.Dispatch(new PlayerFinishedEvent(
                participant.Nick,
                command.LobbyId,
                command.UserId,
                participant.Result!.FinishPosition,
                participant.Result.WPM,
                participant.Result.Accuracy));
        }

        if (race.IsRaceOver())
        {
            _raceTickRegistry.DeregisterLobby(lobby.Id);

            var results = race.GetRaceStatics();

            await _eventDispatcher.Dispatch(new RaceFinishedEvent(command.LobbyId, results));
        }

    }
}
