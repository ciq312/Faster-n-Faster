namespace FasterNFaster.Api.Infrastructure.Auth;

public class VerifyEmailOptions
{
    public TimeSpan ExpirationTime { get; set; } = TimeSpan.FromHours(1);
}