using System.ComponentModel.DataAnnotations;
using FasterNFaster.Api.Core.Interfaces;
using MiniValidationPlus;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace FasterNFaster.Api.Core.Entities;

public class User
{
    public Guid Id { get; private set; }

    [StringLength(30, MinimumLength = 1), Required]
    public string Nick { get; private set; }

    [StringLength(100, MinimumLength = 4)]
    public string? Login { get; private set; }

    [StringLength(100, MinimumLength = 4)]
    public string? Password { get; private set; }

    public string? Token { get; private set; }

    public bool IsAnonymous => Login == null;

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
        Token = null;

        if (!MiniValidatorPlus.TryValidate(this, out var errors))
            throw new ValidationException(string.Join("; ", errors.Values.SelectMany(e => e)));
    }

    public void SetPassword(string newPassword)
    {
        Password = newPassword;
    }

}
