using FasterNFaster.Api.Infrastructure.Smtp.EmailSender;

namespace FasterNFaster.Tests.Fakes;

public class FakeEmailSender : IEmailSender
{
    public record SentEmail(string Name, string Email, string Token);
    public List<SentEmail> Sent { get; } = new();
    public List<SentEmail> SentPasswordResets { get; } = new();

    public Task SendConfirmationEmail(string receiverName, string receiverEmailAddress, string verificationToken)
    {
        Sent.Add(new SentEmail(receiverName, receiverEmailAddress, verificationToken));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmail(string receiverName, string receiverEmailAddress, string resetToken)
    {
        SentPasswordResets.Add(new SentEmail(receiverName, receiverEmailAddress, resetToken));
        return Task.CompletedTask;
    }
}
