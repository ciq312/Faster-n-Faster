namespace FasterNFaster.Api.UseCases.Interfaces.Auth;

public interface IEmailSender
{
    Task SendConfirmationEmail(string receiverName, string receiverEmailAddress, string verificationToken);
    Task SendPasswordResetEmail(string receiverName, string receiverEmailAddress, string resetToken);
}
