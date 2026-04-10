namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public record ParticipantSnapshot(Guid PlayerId, int Index, double Wpm, string Color, string Nick);

public abstract class Race // ISession in future when new mechanics implemeneted
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }

    private readonly Dictionary<Guid, RaceParticipant> _participants = new();
    public IReadOnlyDictionary<Guid, RaceParticipant> Participants => _participants;

    protected readonly object _raceLock = new();
    protected int _nextFinishPosition = 1;


    public virtual void Start(IEnumerable<(Guid Id, string Color, string nick)> players)
    {
        _participants.Clear();
        _nextFinishPosition = 1;

        StartTime = DateTime.UtcNow;

        // in future for players that are not spectators 
        foreach (var (Id, Color, nick) in players)
            _participants[Id] = new RaceParticipant(Id, Color, nick);
    }

    /// <summary>
    /// Validates a client state snapshot and checks for finish.
    /// Thread-safe — called from SignalR hub threads.
    /// </summary>
    public abstract void ProcessUpdate(Guid playerId, int index, int mistakes);

    /// <summary>
    /// Returns a thread-safe snapshot of all participants for the tick service.
    /// </summary>
    public abstract List<ParticipantSnapshot> GetSnapshot();

    public IEnumerable<RaceParticipantResult> GetRaceStatics()
    {
        List<RaceParticipantResult> results = [];
        foreach (var participant in Participants) results.Add(participant.Value.Result!);
        return results;
    }

    public void WithdrawParticipant(Guid playerId)
    {
        lock (_raceLock)
        {
            var participant = _participants.GetValueOrDefault(playerId);
            if (participant is null || participant.IsFinished) return;
            participant.MarkWithdrawn();
        }
    }

    public abstract IRaceSettings GetRaceSettings();

    public bool IsRaceFinished
    {
        get { lock (_raceLock) return _participants.Values.All(p => p.IsFinished); }
    }
}
