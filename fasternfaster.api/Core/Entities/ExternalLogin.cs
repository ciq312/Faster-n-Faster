namespace FasterNFaster.Api.Core.Entities;

public class ExternalLogin
{
    public Guid Id { get; private set; }

    public Guid UserId { get; private set; }
    public string? ExternalSubject { get; private set; }
    public string? ExternalEmail { get; private set; }
    public string? Provider { get; private set; }

    public readonly DateTime CreatedAt;

    public ExternalLogin(Guid userId, string? externalSubject, string? externalEmail, string? provider)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        ExternalEmail = externalEmail;
        ExternalSubject = externalSubject;
        Provider = provider;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateLogin(string externalEmail)
    {
        ExternalEmail = externalEmail;
    }
}