namespace FasterNFaster.Api.UseCases.Auth.Ports;

public interface IEmailSender
{
    Task SendConfirmationEmail(string receiverName, string receiverEmailAddress, string verificationToken);
    Task SendPasswordResetEmail(string receiverName, string receiverEmailAddress, string resetToken);
}
