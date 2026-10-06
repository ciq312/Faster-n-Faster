namespace FasterNFaster.Api.Core.Entities;

public class User : Entity<Guid>
{
    public string? Email { get; private set; }
    public string Nick { get; private set; }
    public string? Login { get; private set; }
    public string? Password { get; private set; }
    public bool IsEmailVerified { get; private set; } = false;
    public readonly DateTime CreatedAt;
    public PlayerStatistics? Statistics { get; private set; }

    /// <summary>Anonymous user with a chosen nick.</summary>
    public User(string nick) : this(nick, null, null) { }

    public User(string nick, string? login, string? password)
    {
        Id = Guid.NewGuid();
        Nick = nick;
        Login = login is null ? null : NormalizeLogin(login);
        Password = password;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetPassword(string newPassword)
    {
        Password = newPassword;
    }

    public void SetEmail(string newEmail)
    {
        Email = NormalizeEmail(newEmail);
    }

    public static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();

    public static string NormalizeLogin(string login) => login.Trim().ToLowerInvariant();

    public void SetEmailVerified()
    {
        IsEmailVerified = true;
    }
}
