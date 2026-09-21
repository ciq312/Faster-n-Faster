using FasterNFaster.Api.Core.Entities.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceInternals
{
    public Task RefreshPassage(Guid lobbyId);

    public Task WithdrawParticipant(Guid lobbyId, Guid userId);

    public Task AddParticipants(Guid lobbyId, List<RaceParticipant> participants);

    public Task StartRace(Guid lobbyId);
}