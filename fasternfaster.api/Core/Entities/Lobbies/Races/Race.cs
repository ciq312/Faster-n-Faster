namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public record ParticipantSnapshot(Guid PlayerId, int Index, string Typed, double Wpm, string Color, string Nick);

public abstract class Race // ISession in future when new mechanics implemeneted
{
    public DateTime StartTime { get; private set; }
    public DateTime EndTime { get; private set; }
    public bool HasStarted { get; private set; }

    private readonly Dictionary<Guid, RaceParticipant> _participants = new();
    public IReadOnlyDictionary<Guid, RaceParticipant> Participants => _participants;

    protected readonly object _raceLock = new();
    protected int _nextFinishPosition = 1;


    public virtual void AddParticipant(RaceParticipant participant)
    {
        if (HasStarted) throw new InvalidOperationException("Cannot add participants to a started race.");
        _participants[participant.Id] = participant;
    }

    public virtual void Start()
    {
        if (HasStarted) throw new InvalidOperationException("Race already started.");
        StartTime = DateTime.UtcNow;
        HasStarted = true;
    }

    public virtual void Reset()
    {
        _participants.Clear();
        _nextFinishPosition = 1;
        HasStarted = false;
    }

    /// <summary>
    /// Validates a client state snapshot and checks for finish.
    /// Thread-safe — called from SignalR hub threads.
    /// </summary>
    public abstract void ProcessUpdate(Guid playerId, int index, int mistakes, string typed);

    /// <summary>
    /// Returns a thread-safe snapshot of all participants for the tick service.
    /// </summary>
    public abstract List<ParticipantSnapshot> GetSnapshot();

    public IEnumerable<RaceParticipantResult> GetRaceStatics()
    {
        lock (_raceLock)
        {
            return _participants.Values
                .Where(p => p.Result != null)
                .Select(p => p.Result!)
                .ToList();
        }
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
