namespace FasterNFaster.Api.Core.Entities.Auth;

public class Token
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string Value { get; set; }
    public TokenType Type { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }

    public bool TryVerify()
    {
        return DateTime.UtcNow < ExpiresAt;
    }
}
