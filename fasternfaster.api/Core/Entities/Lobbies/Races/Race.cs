namespace FasterNFaster.Api.Core.Entities.Lobbies.Races;

public record ParticipantSnapshot(Guid PlayerId, int Index, double Wpm, string Color);

public abstract class Race
{
    private static readonly string[] WordsPool =
    [
        "the", "be", "to", "of", "and", "a", "in", "that", "have", "it",
        "for", "not", "on", "with", "he", "as", "you", "do", "at", "this",
        "but", "his", "by", "from", "they", "we", "say", "her", "she", "or",
        "an", "will", "my", "one", "all", "would", "there", "their", "what",
        "so", "up", "out", "if", "about", "who", "get", "which", "go", "me",
        "when", "make", "can", "like", "time", "no", "just", "him", "know",
        "take", "people", "into", "year", "your", "good", "some", "could",
        "them", "see", "other", "than", "then", "now", "look", "only", "come",
        "its", "over", "think", "also", "back", "after", "use", "two", "how",
        "our", "work", "first", "well", "way", "even", "new", "want", "because",
        "any", "these", "give", "day", "most", "us", "great", "between", "need",
        "large", "often", "around", "each", "still", "every", "point", "keep",
        "never", "last", "long", "same", "another", "much", "while", "before"
    ];

    public DateTime StartTime { get; private set; }
    public string Words { get; private set; } = "";
    public int MaxWords { get; protected set; }

    private readonly Dictionary<Guid, RaceParticipant> _participants = new();
    public IReadOnlyDictionary<Guid, RaceParticipant> Participants => _participants;
    private readonly object _raceLock = new();
    private int _nextFinishPosition = 1;


    public virtual void Start(IEnumerable<(Guid Id, string Color, string nick)> players)
    {
        GenerateWords();
        StartTime = DateTime.UtcNow;

        foreach (var player in players)
            _participants[player.Id] = new RaceParticipant(player.Id, player.Color, player.nick);
    }

    /// <summary>
    /// Validates a client state snapshot and checks for finish.
    /// Thread-safe — called from SignalR hub threads.
    /// </summary>
    public RaceParticipant? ProcessUpdate(Guid playerId, int index, int totalTyped, int mistakes)
    {
        lock (_raceLock)
        {
            var participant = _participants.GetValueOrDefault(playerId)
                ?? throw new InvalidOperationException("Player is not a race participant.");

            if (!participant.ValidateUpdate(index, totalTyped, mistakes, Words.Length))
                return null;

            if (!participant.IsFinished && participant.Index >= Words.Length)
                participant.MarkFinished(_nextFinishPosition++);

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
                .Select(p => new ParticipantSnapshot(p.Id, p.Index, p.GetWPM(), p.Color))
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
    private void GenerateWords()
    {
        var selected = new string[MaxWords];
        for (int i = 0; i < MaxWords; i++)
            selected[i] = WordsPool[Random.Shared.Next(WordsPool.Length)];

        Words = string.Join(" ", selected);
    }
}
