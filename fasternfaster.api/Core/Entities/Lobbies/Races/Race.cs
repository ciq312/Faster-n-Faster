using FasterNFaster.Api.Core.Entities.Lobbies.Races.Events;

namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public record ParticipantSnapshot(Guid PlayerId, int Index, string Typed, double Wpm, string Color, string Nick, int Mistakes);

public abstract class Race : AggregateRoot // ISession in future when new mechanics implemeneted
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public bool HasStarted { get; private set; }

    private readonly Dictionary<Guid, RaceParticipant> participants = new();
    public IReadOnlyDictionary<Guid, RaceParticipant> Participants => participants;

    protected int nextFinishPosition = 1;


    public void AddParticipant(RaceParticipant participant)
    {
        if (HasStarted) throw new InvalidOperationException("Cannot add participants to a started race.");
        participants[participant.Id] = participant;
    }

    public void AddParticipants(IEnumerable<RaceParticipant> newParticipants)
    {
        foreach (var participant in newParticipants)
        {
            AddParticipant(participant);
        }
    }
    public virtual void Start()
    {
        if (HasStarted) throw new InvalidOperationException("Race already started.");
        StartTime = DateTime.UtcNow;
        HasStarted = true;
    }

    public virtual void Reset()
    {
        participants.Clear();
        nextFinishPosition = 1;
        HasStarted = false;
    }
    public abstract void ProcessUpdate(Guid playerId, int index, int mistakes, string typed);

    public abstract List<ParticipantSnapshot> GetSnapshot();

    public IEnumerable<RaceParticipantResult> GetRaceResults() =>
        participants.Values
            .Where(p => p.Result != null)
            .Select(p => p.Result!);



    public void WithdrawParticipant(Guid playerId)
    {
        var participant = participants.GetValueOrDefault(playerId);
        if (participant is null || participant.IsFinished) return;
        participant.MarkWithdrawn();
        if (IsRaceFinished())
            OnRaceFinished();

    }

    public abstract IRaceSettings GetRaceSettings();

    protected void OnRaceFinished()
    {
        RaiseDomainEvent(new RaceFinishedEvent(GetRaceResults().ToList()));
        EndTime = DateTime.UtcNow;
        Reset();
    }
    public bool IsRaceFinished() => participants.Values.All(p => p.IsFinished);

}
