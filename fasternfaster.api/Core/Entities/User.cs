using System.ComponentModel.DataAnnotations;
using FasterNFaster.Api.Core.Interfaces;
using MiniValidation;
using MiniValidationPlus;
using ValidationException = System.ComponentModel.DataAnnotations.ValidationException;

namespace FasterNFaster.Api.Core.Entities;

public class User : IEntity
{
    public Guid Id { get; private set; }
    public string Token { get; private set; } = null!;

    [StringLength(30, MinimumLength = 1), Required]
    public string Nick { get; private set; }

    [StringLength(100, MinimumLength = 4)]
    public string? Login { get; private set; }

    [StringLength(100, MinimumLength = 4)]
    public string? Password { get; private set; }

    public bool IsAnonymous => Login == null;


    /// <summary>Anonymous user with a chosen nick.</summary>
    public User(string nick) : this(nick, null, null) { }

    public User(string nick, string? login, string? password)
    {
        Id = Guid.NewGuid();
        Token = GenerateToken();
        Nick = nick;
        Login = login;
        Password = password;

        if (!MiniValidatorPlus.TryValidate(this, out var errors))
            throw new ValidationException(string.Join("; ", errors.Values.SelectMany(e => e)));
    }

    private static string GenerateToken() =>
        Convert.ToBase64String(Guid.NewGuid().ToByteArray()) +
        Convert.ToBase64String(Guid.NewGuid().ToByteArray());
}
