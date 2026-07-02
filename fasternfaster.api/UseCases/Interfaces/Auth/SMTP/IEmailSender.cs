namespace FasterNFaster.Api.UseCases.Interfaces;

public interface IEmailSender
{
    Task SendConfirmationEmail(string receiverName, string receiverEmailAddress, string verificationToken);
    Task SendPasswordResetEmail(string receiverName, string receiverEmailAddress, string resetToken);
}
