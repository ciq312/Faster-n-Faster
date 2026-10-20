namespace FasterNFaster.Api.Domain.Entities.Lobbies;

public class LobbySettings
{
    private const int CODE_LENGTH = 6;

    private static readonly char[] AlphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public int MaxPlayers { get; private set; } = 10;
    public bool IsPrivate { get; private set; }
    public string InviteCode { get; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    private readonly TimeProvider timeProvider;

    public LobbySettings(bool isPrivate, Func<string, bool> inviteCodeExists, TimeProvider timeProvider)
    {
        this.timeProvider = timeProvider;
        IsPrivate = isPrivate;
        InviteCode = CreateUniqueInviteCode(inviteCodeExists);
        var now = timeProvider.GetUtcNow();
        CreatedAt = now;
        UpdatedAt = now;
    }

    public void UpdateTimestamp()
    {
        UpdatedAt = timeProvider.GetUtcNow();
    }

    private string CreateUniqueInviteCode(Func<string, bool> codeExists)
    {

        while (true)
        {
            var code = constructCode();

            if (!codeExists(code))
                return code;
        }
    }

    private string constructCode() => string.Create(
                CODE_LENGTH,
                Random.Shared,
                (span, rng) =>
                {
                    for (var i = 0; i < span.Length; i++)
                        span[i] = AlphanumericChars[rng.Next(AlphanumericChars.Length)];
                }
            );
}
