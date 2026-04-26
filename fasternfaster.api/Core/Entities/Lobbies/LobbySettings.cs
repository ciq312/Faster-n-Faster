namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class LobbySettings
{
    const int CODE_LENGTH = 6;

    private static readonly char[] AlphanumericChars =
        "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToCharArray();

    public int MaxPlayers { get; private set; } = 10;
    public bool IsPrivate { get; private set; }
    public string? InviteCode { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public LobbySettings(bool isPrivate)
    {
        IsPrivate = isPrivate;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }

    public string CreateUniqueInviteCode(Func<string, bool> codeExists)
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

    public void SetInviteCode(string code)
    {
        InviteCode = code;
        UpdateTimestamp();
    }
}
