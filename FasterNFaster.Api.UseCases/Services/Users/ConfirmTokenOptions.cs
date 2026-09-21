using System.ComponentModel;
using FasterNFaster.Api.Core.Entities.Auth;

namespace FasterNFaster.Api.UseCases.Services.Users;

public class ConfirmTokenOptions
{
    public ConfirmTokenPolicy EmailVerification { get; set; } = new() { ExpirationTime = TimeSpan.FromHours(1) };
    public ConfirmTokenPolicy PasswordReset { get; set; } = new() { ExpirationTime = TimeSpan.FromMinutes(15) };

    public ConfirmTokenPolicy For(TokenType type) => type switch
    {
        TokenType.EmailVerification => EmailVerification,
        TokenType.PasswordReset => PasswordReset,
        _ => throw new InvalidEnumArgumentException(nameof(type), (int)type, typeof(TokenType)),
    };
}

public class ConfirmTokenPolicy
{
    public TimeSpan ExpirationTime { get; set; }
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(15);
}
