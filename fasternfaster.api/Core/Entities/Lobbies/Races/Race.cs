namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public record ParticipantSnapshot(Guid PlayerId, int Index, double Wpm, string Color, string Nick);

public abstract class Race
{
    public DateTime StartTime { get; private set; }
    public string Words { get; private set; } = "";
    public int MaxWords { get; protected set; }
    private string? _passage;

    private readonly Dictionary<Guid, RaceParticipant> _participants = new();
    public IReadOnlyDictionary<Guid, RaceParticipant> Participants => _participants;
    private readonly object _raceLock = new();
    private int _nextFinishPosition = 1;

    public void SetPassage(string passage)
    {
        _passage = passage;
        Words = passage;
    }

    public virtual void Start(IEnumerable<(Guid Id, string Color, string nick)> players)
    {
        _participants.Clear();
        _nextFinishPosition = 1;

        if (_passage != null)
            Words = _passage;

        StartTime = DateTime.UtcNow;

        foreach (var player in players)
            _participants[player.Id] = new RaceParticipant(player.Id, player.Color, player.nick);
    }

    /// <summary>
    /// Validates a client state snapshot and checks for finish.
    /// Thread-safe — called from SignalR hub threads.
    /// </summary>
    public RaceParticipant? ProcessUpdate(Guid playerId, int index, int mistakes)
    {
        lock (_raceLock)
        {
            var participant = _participants.GetValueOrDefault(playerId)
                ?? throw new InvalidOperationException("Player is not a race participant.");

            if (!participant.ValidateUpdate(index, mistakes, Words.Length))
                return null;

            if (!participant.IsFinished && participant.Index >= Words.Length - 1)
                participant.MarkFinished(_nextFinishPosition++, Words.Split(' ').Length);

            return participant;
        }
    }

    /// <summary>
    /// Returns a thread-safe snapshot of all participants for the tick service.
    /// </summary>
    public List<ParticipantSnapshot> GetSnapshot()
    {
        lock (_raceLock)
        {
            return _participants.Values
                .Where(p => !p.IsFinished)
                .Select(p => new ParticipantSnapshot(p.Id, p.Index, p.GetWPM(), p.Color, p.Nick))
                .ToList();
        }
    }

    public bool IsRaceOver()
    {
        lock (_raceLock)
        {
            return _participants.Values.All(p => p.IsFinished);
        }
    }

    public void RaceFinished()
    {
        if (!IsRaceOver()) throw new InvalidOperationException("can't call finished race hasn't finished yet");

        // foreach (var participant in Participants) 
    }

    public IEnumerable<RaceParticipantResult> GetRaceStatics()
    {
        List<RaceParticipantResult> results = [];
        foreach (var participant in Participants) results.Add(participant.Value.Result!);
        return results;
    }
}
