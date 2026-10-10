namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class LobbySettings
{
    private const int CODE_LENGTH = 6;

    private static readonly char[] AlphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public int MaxPlayers { get; private set; } = 10;
    public bool IsPrivate { get; private set; }
    public string InviteCode { get; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public LobbySettings(bool isPrivate, Func<string, bool> inviteCodeExists)
    {
        IsPrivate = isPrivate;
        InviteCode = CreateUniqueInviteCode(inviteCodeExists);
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
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
