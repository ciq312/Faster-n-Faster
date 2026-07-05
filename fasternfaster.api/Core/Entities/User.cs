namespace FasterNFaster.Api.Core.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string? Email { get; private set; }
    public string Nick { get; private set; }
    public string? Login { get; private set; }
    public string? Password { get; private set; }
    public bool IsAnonymous => Login == null;
    public bool IsEmailVerified { get; private set; } = false;
    public readonly DateTime CreatedAt;
    public PlayerStatistics? Statistics { get; private set; }

    public static User Guest(Guid id, string nick)
    {
        var user = new User(nick)
        {
            Id = id
        };
        return user;
    }

    /// <summary>Anonymous user with a chosen nick.</summary>
    public User(string nick) : this(nick, null, null) { }

    public User(string nick, string? login, string? password)
    {
        Id = Guid.NewGuid();
        Nick = nick;
        Login = login;
        Password = password;
        CreatedAt = DateTime.UtcNow;
    }

    public void SetPassword(string newPassword)
    {
        Password = newPassword;
    }

    public void SetEmail(string newEmail)
    {
        Email = newEmail;
    }

    public void SetEmailVerified()
    {
        IsEmailVerified = true;
    }
}
