namespace FasterNFaster.Api.Core.Entities.Lobby;

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
    private int _nextFinishPosition = 1;

    public virtual void Start(IEnumerable<Guid> playerIds)
    {
        GenerateWords();
        StartTime = DateTime.UtcNow;

        foreach (var playerId in playerIds)
            _participants[playerId] = new RaceParticipant(playerId);
    }

    public RaceParticipant? GetParticipant(Guid playerId) =>
        _participants.GetValueOrDefault(playerId);

    /// <summary>
    /// Validates a client state snapshot and checks for finish.
    /// </summary>
    /// <returns>The participant if update was accepted, null if rejected</returns>
    public RaceParticipant? ProcessUpdate(Guid playerId, int index, int totalTyped, int mistakes)
    {
        var participant = GetParticipant(playerId)
            ?? throw new InvalidOperationException("Player is not a race participant.");

        if (!participant.ValidateUpdate(index, totalTyped, mistakes, Words.Length))
            return null;

        // Server-side finish detection
        if (!participant.IsFinished && participant.Index >= Words.Length)
            participant.MarkFinished(_nextFinishPosition++);

        return participant;
    }

    public bool IsRaceOver() =>
        _participants.Values.All(p => p.IsFinished);

    private void GenerateWords()
    {
        var selected = new string[MaxWords];
        for (int i = 0; i < MaxWords; i++)
            selected[i] = WordsPool[Random.Shared.Next(WordsPool.Length)];

        Words = string.Join(" ", selected);
    }
}
