namespace FasterNFaster.Api.Core.Entities.Lobbies;

public class LobbySettings
{
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

    public static async Task<string> GenerateUniqueInviteCode(Func<string, Task<bool>> codeExists)
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

    public void SetInviteCode(string code)
    {
        InviteCode = code;
        UpdateTimestamp();
    }
}
