using FasterNFaster.Api.Core.Entities.Lobbies.Races;

namespace FasterNFaster.Api.UseCases.Interfaces.Races;

public interface IRaceInternals
{
    public Task RefreshPassage(Guid lobbyId);

    public void WithdrawParticipant(Guid lobbyId, Guid userId);

    public void AddParticipants(Guid lobbyId, List<RaceParticipant> participants);

    public void StartRace(Guid lobbyId);
}