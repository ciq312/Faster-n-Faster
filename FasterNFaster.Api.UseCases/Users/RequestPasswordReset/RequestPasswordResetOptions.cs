namespace FasterNFaster.Api.UseCases.Users.RequestPasswordReset;

public class RequestPasswordResetOptions
{
    public TimeSpan Cooldown { get; set; } = TimeSpan.FromSeconds(15);
}
