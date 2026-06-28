namespace FasterNFaster.Api.UseCases.Users.ResendVerification;

public class ResendVerificationOptions
{
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(15);
}
