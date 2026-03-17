using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FasterNFaster.Api.Core.Entities;

[Table("lobbies")]
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

    [Column("id")]
    public Guid Id { get; private set; }

    [Column("name")]
    [MaxLength(100)]
    public string Name { get; private set; } = null!;

    [Column("is_private")]
    public bool IsPrivate { get; private set; }

    [Column("invite_code")]
    [MaxLength(8)]
    public string? InviteCode { get; private set; }

    [Column("status")]
    public string Status { get; private set; } = "waiting";

    [Column("host_player_id")]
    public Guid? HostPlayerId { get; private set; }

    [Column("max_players")]
    public int MaxPlayers { get; private set; } = 30;

    [Column("created_at")]
    public DateTime CreatedAt { get; private set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; private set; }

    public WordRace? WordRace { get; private set; }
    public TimerRace? TimerRace { get; private set; }

    public ICollection<LobbyPlayer> Players { get; private set; } = new List<LobbyPlayer>();
    public ICollection<RaceResult> RaceResults { get; private set; } = new List<RaceResult>();

    private Lobby() { } // EF constructor

    /// <param name="nameExists">Checks whether an active (non-finished) lobby with this name already exists.</param>
    /// <param name="inviteCodeExists">Checks whether a lobby with this invite code already exists.</param>
    public static async Task<Lobby> Create(
        string? name,
        bool isPrivate,
        Func<string, Task<bool>> nameExists,
        Func<string, Task<bool>> inviteCodeExists)
    {
        var lobbyName = string.IsNullOrWhiteSpace(name) ? "New Lobby" : name;

        if (lobbyName.Length > 100)
            throw new ArgumentException("Lobby name must be 100 characters or fewer.");

        if (await nameExists(lobbyName))
            throw new InvalidOperationException($"A lobby named '{lobbyName}' already exists.");

        string? inviteCode = null;
        if (isPrivate)
            inviteCode = await GenerateUniqueInviteCode(inviteCodeExists);

        return new Lobby
        {
            Id = Guid.NewGuid(),
            Name = lobbyName,
            IsPrivate = isPrivate,
            InviteCode = inviteCode,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };
    }

    public IRace GetRace() =>
        (IRace?)WordRace ?? TimerRace
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
                $"Cannot transition from '{Status}' to '{newStatus}'.");

        Status = newStatus;
        UpdatedAt = DateTime.UtcNow;
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
            var code = string.Create(codeLength, Random.Shared, (span, rng) =>
            {
                for (var i = 0; i < span.Length; i++)
                    span[i] = AlphanumericChars[rng.Next(AlphanumericChars.Length)];
            });

            if (!await codeExists(code))
                return code;
        }
    }
}
