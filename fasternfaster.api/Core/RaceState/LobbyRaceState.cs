using FasterNFaster.Api.Core.Entities;

namespace FasterNFaster.Api.Core.RaceState;

public class LobbyRaceState
{
    public Guid LobbyId { get; }
    public IRace Race { get; }
    public string Passage { get; }
    public DateTime StartedAt { get; }
    public IReadOnlyDictionary<Guid, RaceParticipant> Participants => _participants;

    private readonly Dictionary<Guid, RaceParticipant> _participants;

    public LobbyRaceState(Guid lobbyId, IRace race, string passage, IEnumerable<RaceParticipant> participants)
    {
        LobbyId = lobbyId;
        Race = race;
        Passage = passage;
        StartedAt = DateTime.UtcNow;
        _participants = participants.ToDictionary(p => p.PlayerId);
    }

    public RaceParticipant? GetParticipant(Guid playerId) =>
        _participants.GetValueOrDefault(playerId);
}
