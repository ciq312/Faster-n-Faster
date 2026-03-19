namespace FasterNFaster.Api.Core.Entities;

public class Lobby
{
    private static readonly char[] AlphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();

    // waiting -> racing -> finished
    private static readonly Dictionary<string, string> AllowedTransitions = new()
    {
        { "waiting", "racing" },
        { "racing", "finished" },
    };

    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public bool IsPrivate { get; private set; }
    public string? InviteCode { get; private set; }
    public string Status { get; private set; } = "waiting";
    public Guid? HostPlayerId { get; private set; }
    public int MaxPlayers { get; private set; } = 30;
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public WordRace? WordRace { get; private set; }
    public TimerRace? TimerRace { get; private set; }

    public ICollection<LobbyPlayer> Players { get; private set; } = new List<LobbyPlayer>();
    public ICollection<RaceResult> RaceResults { get; private set; } = new List<RaceResult>();

    public Lobby(string name, bool isPrivate)
    {
        Id = Guid.NewGuid();
        Name = name;
        IsPrivate = isPrivate;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public IRace GetRace() =>
        (IRace?)WordRace
        ?? TimerRace
        ?? throw new InvalidOperationException("Race has not been configured.");

    public void ConfigureWordRace(int wordCount)
    {
        ClearRace();
        WordRace = new WordRace(wordCount);
        UpdatedAt = DateTime.UtcNow;
    }

    public void ConfigureTimerRace(int timerDurationSeconds)
    {
        ClearRace();
        TimerRace = new TimerRace(timerDurationSeconds);
        UpdatedAt = DateTime.UtcNow;
    }

    public void TransitionStatus(string newStatus)
    {
        if (!AllowedTransitions.TryGetValue(Status, out var expected) || expected != newStatus)
            throw new InvalidOperationException(
                $"Cannot transition from '{Status}' to '{newStatus}'."
            );

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
    }

    private readonly object _lock = new();

    public LobbyPlayer AddPlayer(Guid userId)
    {
        lock (_lock)
        {
            if (Status != "waiting")
                throw new InvalidOperationException("Lobby is not accepting players.");

            if (Players.Count >= MaxPlayers)
                throw new InvalidOperationException("Lobby is full.");

            if (IsPrivate && HostPlayerId != userId)
                throw new InvalidOperationException("Cannot join a private lobby.");

            if (Players.Any(p => p.PlayerId == userId))
                throw new InvalidOperationException("Player is already in this lobby.");

            var joinOrder = Players.Any() ? Players.Max(p => p.JoinOrder) + 1 : 1;
            var player = new LobbyPlayer(userId, Id, joinOrder);
            Players.Add(player);
            UpdatedAt = DateTime.UtcNow;
            return player;
        }
    }

    public void AssignHost(Guid? playerId)
    {
        HostPlayerId = playerId;
        UpdatedAt = DateTime.UtcNow;
    }

    private void ClearRace()
    {
        WordRace = null;
        TimerRace = null;
    }

    private static async Task<string> GenerateUniqueInviteCode(Func<string, Task<bool>> codeExists)
    {
        const int codeLength = 6;

        while (true)
        {
            var code = string.Create(
                codeLength,
                Random.Shared,
                (span, rng) =>
                {
                    for (var i = 0; i < span.Length; i++)
                        span[i] = AlphanumericChars[rng.Next(AlphanumericChars.Length)];
                }
            );

            if (!await codeExists(code))
                return code;
        }
    }
}
