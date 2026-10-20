namespace FasterNFaster.Api.Domain.Entities.Auth;

public class Token
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public required string Value { get; set; }
    public TokenType Type { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }

    public bool IsValid(DateTimeOffset now)
    {
        return now < ExpiresAt;
    }
}
