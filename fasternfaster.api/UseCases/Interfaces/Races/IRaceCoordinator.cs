using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceCoordinator
{
    public Task RefreshPassage(Guid lobbyId);

    public Task WithdrawParticipant(Guid lobbyId, Guid userId);

    public Task AddParticipants(Guid lobbyId, List<RaceParticipant> participants);

    public Task StartRace(Guid lobbyId);
}