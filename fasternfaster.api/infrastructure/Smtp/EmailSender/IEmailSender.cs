namespace FasterNFaster.Api.Infrastructure.Smtp.EmailSender;

public interface IEmailSender
{
    Task SendConfirmationEmail(string receiverName, string receiverEmailAddress, string verificationToken);
    Task SendPasswordResetEmail(string receiverName, string receiverEmailAddress, string resetToken);
}
