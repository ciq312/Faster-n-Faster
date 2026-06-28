namespace FasterNFaster.Api.Infrastructure.Auth;

public class ResetPasswordOptions
{
    public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromMinutes(15);
}